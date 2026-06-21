using System.Security.Cryptography;
using System.Text;
using Infrastructure.RateLimiting;
using StackExchange.Redis;
using Web.Observability;

namespace Web.RateLimiting;

public sealed class DistributedRateLimitOptions
{
    public const string SectionName = "RateLimiting";
    public bool Enabled { get; set; } = true;
    public string KeyPrefix { get; set; } = "englishai:ratelimit";
    public int PermitPerMinute { get; set; } = 300;
    public int AuthPermitPerMinute { get; set; } = 20;
    public int DeveloperPermitPerMinute { get; set; } = 12;
    public int AiPermitPerMinute { get; set; } = 40;
    public int TranslationPermitPerMinute { get; set; } = 30;
    public int GlobalEmergencyPermitPerMinute { get; set; } = 10000;
    public int CoreReadFallbackPermitPerMinute { get; set; } = 30;
    public int RedisTimeoutMilliseconds { get; set; } = 250;
}

public sealed record RateLimitErrorResponse(string Code, string Message, int RetryAfterSeconds, string CorrelationId);

public sealed class DistributedRateLimitMiddleware(
    RequestDelegate next,
    IDistributedRateLimitStore store,
    DistributedRateLimitOptions options,
    ILogger<DistributedRateLimitMiddleware> logger)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly object _fallbackGate = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _fallbackReads = new(StringComparer.Ordinal);

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        if (!options.Enabled || IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        var policy = ResolvePolicy(context);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(Math.Clamp(options.RedisTimeoutMilliseconds, 50, 5000)));
            var global = await store.AcquireAsync($"{options.KeyPrefix}:global", options.GlobalEmergencyPermitPerMinute, Window, timeout.Token);
            if (!global.Allowed)
            {
                await WriteRejectedAsync(context, "global_rate_limited", "Service traffic limit reached.", global.RetryAfterSeconds);
                return;
            }

            var lease = await store.AcquireAsync($"{options.KeyPrefix}:{policy.Name}:{policy.Key}", policy.PermitLimit, Window, timeout.Token);
            if (!lease.Allowed)
            {
                await WriteRejectedAsync(context, "rate_limited", "Too many requests. Please retry shortly.", lease.RetryAfterSeconds);
                return;
            }
        }
        catch (Exception exception) when (IsStoreFailure(exception, context))
        {
            logger.LogWarning(exception, "Distributed rate limit store unavailable for {Path}", context.Request.Path);
            if (IsCoreRead(context.Request) && TryAcquireFallback(policy.Key, options.CoreReadFallbackPermitPerMinute))
            {
                context.Response.Headers["X-RateLimit-Fallback"] = "local-read";
                await next(context);
                return;
            }
            await WriteUnavailableAsync(context);
            return;
        }

        await next(context);
    }

    private static bool IsStoreFailure(Exception exception, HttpContext context) =>
        exception is RedisException or TimeoutException
        || exception is OperationCanceledException && !context.RequestAborted.IsCancellationRequested;

    private Policy ResolvePolicy(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api/auth"))
            return new Policy("auth", $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}", options.AuthPermitPerMinute);
        if (path.StartsWithSegments("/v1/chat/completions"))
            return new Policy("developer", DeveloperKey(context), options.DeveloperPermitPerMinute);
        if (path.StartsWithSegments("/api/translate"))
            return new Policy("translation", ClientKey(context), options.TranslationPermitPerMinute);
        if (IsAiSurface(context.Request))
            return new Policy("ai", ClientKey(context), options.AiPermitPerMinute);
        return new Policy("core", ClientKey(context), options.PermitPerMinute);
    }

    public static string ClientKey(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value
                      ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrWhiteSpace(subject)
            ? $"user:{subject}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    public static string DeveloperKey(HttpContext context)
    {
        const string bearer = "Bearer ";
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
        {
            var key = authorization[bearer.Length..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
                return $"key:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant()}";
        }
        return ClientKey(context);
    }

    public static bool IsAiSurface(HttpRequest request)
    {
        var path = request.Path;
        if (HttpMethods.IsPost(request.Method) && path.StartsWithSegments("/api/video", out var videoRemainder) &&
            videoRemainder.Value?.EndsWith("/quiz", StringComparison.OrdinalIgnoreCase) == true &&
            Guid.TryParse(videoRemainder.Value.Split('/')[1], out _)) return true;
        if (path.StartsWithSegments("/api/video/explain")) return true;
        if (path.StartsWithSegments("/api/translate")) return true;
        if (path.StartsWithSegments("/api/assistant/ask")
            || path.StartsWithSegments("/api/assistant/project")
            || path.StartsWithSegments("/api/assistant/context/stream")) return true;
        if (path.StartsWithSegments("/api/assistant/sessions", out var assistantRemainder))
            return HttpMethods.IsPost(request.Method)
                   && assistantRemainder.Value?.EndsWith("/messages/stream", StringComparison.OrdinalIgnoreCase) == true;
        if (path.StartsWithSegments("/api/writing/topic") || path.StartsWithSegments("/api/writing/submit")) return true;
        if (!path.StartsWithSegments("/api/speaking", out var speakingRemainder)) return false;

        var speakingPath = speakingRemainder.Value ?? string.Empty;
        return speakingPath.Equals("/start", StringComparison.OrdinalIgnoreCase)
               || speakingPath.StartsWith("/utterance", StringComparison.OrdinalIgnoreCase)
               || speakingPath.StartsWith("/practice-words/", StringComparison.OrdinalIgnoreCase)
                  && speakingPath.EndsWith("/attempt", StringComparison.OrdinalIgnoreCase)
               || speakingPath.Equals("/segment-pronunciation", StringComparison.OrdinalIgnoreCase)
               || speakingPath.Equals("/idea-cards", StringComparison.OrdinalIgnoreCase)
               || speakingPath.Equals("/roleplay/start", StringComparison.OrdinalIgnoreCase)
               || speakingPath.Equals("/roleplay/evaluate", StringComparison.OrdinalIgnoreCase)
               // Minting a Voice Live token is the one server round trip a realtime session
               // needs, so it is the only place the surface can be throttled. Its sibling
               // /voice-live/complete is intentionally left out: throttling the settle path
               // strands the reservation it was meant to refund.
               || (speakingPath.StartsWith("/accent-tutors/", StringComparison.OrdinalIgnoreCase)
                   && speakingPath.EndsWith("/voice-live/token", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsExempt(PathString path) =>
        path.StartsWithSegments("/hubs") || path.StartsWithSegments("/health")
        || path.StartsWithSegments("/metrics") || path.StartsWithSegments("/api/images")
        || path.StartsWithSegments("/api/placement/audio")
        || (path.Value?.EndsWith("/audio", StringComparison.Ordinal) ?? false);

    private static bool IsCoreRead(HttpRequest request) =>
        (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
        && !request.Path.StartsWithSegments("/api/auth") && !request.Path.StartsWithSegments("/v1")
        && !IsAiSurface(request);

    private bool TryAcquireFallback(string key, int limit)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_fallbackGate)
        {
            if (!_fallbackReads.TryGetValue(key, out var queue))
                _fallbackReads[key] = queue = new Queue<DateTimeOffset>();
            while (queue.TryPeek(out var timestamp) && now - timestamp >= Window) queue.Dequeue();
            if (queue.Count >= Math.Max(1, limit)) return false;
            queue.Enqueue(now);
            return true;
        }
    }

    private static async Task WriteRejectedAsync(HttpContext context, string code, string message, int retryAfterSeconds)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = Math.Max(1, retryAfterSeconds).ToString();
        await context.Response.WriteAsJsonAsync(new RateLimitErrorResponse(code, message, Math.Max(1, retryAfterSeconds), CorrelationId(context)), context.RequestAborted);
    }

    private static async Task WriteUnavailableAsync(HttpContext context)
    {
        const int retryAfterSeconds = 5;
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        await context.Response.WriteAsJsonAsync(new RateLimitErrorResponse(
            "rate_limit_store_unavailable", "Request protection is temporarily unavailable. Please retry shortly.", retryAfterSeconds, CorrelationId(context)), context.RequestAborted);
    }

    private static string CorrelationId(HttpContext context) =>
        context.Items.TryGetValue(CorrelationConstants.ItemKey, out var value)
            ? value?.ToString() ?? context.TraceIdentifier : context.TraceIdentifier;

    private sealed record Policy(string Name, string Key, int PermitLimit);
}
