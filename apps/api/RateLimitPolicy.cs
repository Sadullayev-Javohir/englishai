using System.Security.Claims;

namespace Web;

/// <summary>
/// Decisions that drive the perimeter rate limiter (registered in <c>Program</c>). Pulled out of
/// the top-level statements so the two pieces of custom logic - which requests are exempt, and how
/// a caller is bucketed - are unit-testable without booting the whole app.
/// </summary>
public static class RateLimitPolicy
{
    /// <summary>
    /// Requests that must never be throttled: the real-time hubs and media/health endpoints
    /// legitimately make many or long-lived requests, so limiting them would break the app.
    /// </summary>
    public static bool IsExempt(PathString path) =>
        path.StartsWithSegments("/hubs")
        || path.StartsWithSegments("/health")
        || path.StartsWithSegments("/api/images")
        // Audio clips are streamed by a plain <audio> element that may issue several range requests.
        // Listening puts /audio at the tail (/api/listening/topic/{id}/audio); placement puts it
        // mid-path (/api/placement/audio/{id}), so match both shapes.
        || path.StartsWithSegments("/api/placement/audio")
        || (path.Value?.EndsWith("/audio", StringComparison.Ordinal) ?? false);

    /// <summary>The tighter-window sign-in surface (the brute-force target).</summary>
    public static bool IsAuthSurface(PathString path) => path.StartsWithSegments("/api/auth");

    public static bool IsDeveloperApiSurface(PathString path) =>
        path.StartsWithSegments("/v1/chat/completions");

    public static bool IsVideoExplainSurface(PathString path) =>
        path == "/api/video/explain" || path == "/api/video/explain/stream";

    public static string ResolveDeveloperApiKey(HttpContext context)
    {
        const string bearer = "Bearer ";
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
        {
            var key = authorization[bearer.Length..].Trim();
            if (key.StartsWith("eai_", StringComparison.Ordinal) && key.Length >= 12)
                return $"key:{key[..12]}";
        }
        return ResolveClientKey(context);
    }

    /// <summary>
    /// Bucket key: the authenticated user id when present (fair per-user limits - one noisy user
    /// can't starve others), otherwise the forwarded client IP.
    /// </summary>
    public static string ResolveClientKey(HttpContext context)
    {
        var sub = context.User.FindFirst("sub")?.Value
                  ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(sub))
            return $"u:{sub}";
        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
