using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Application.Developer.CreateApiKey;
using Application.Developer.ListApiKeys;
using Application.Developer.Ports;
using Application.Developer.RevokeApiKey;
using Application.Common;
using Application.Identity.Dtos;
using Domain.Developer;
using Infrastructure.Developer;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// Developer credentials plus an OpenAI-compatible inference surface. Session auth manages keys;
/// generated eai_* bearer tokens authenticate /v1 requests.
/// </summary>
public static class DeveloperApiEndpoints
{
    private static SemaphoreSlim? DeveloperConcurrency;
    private static int DeveloperConcurrencyLimit;
    private static readonly object DeveloperConcurrencyLock = new();

    public static IEndpointRouteBuilder MapDeveloperApiEndpoints(this IEndpointRouteBuilder app)
    {
        var management = app.MapGroup("/api/developer").WithTags("Developer API");

        management.MapGet("/keys", async (HttpContext http, ISender sender, IAdminAuthorization admin) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, http.RequestAborted) is not AdminRole.SuperAdmin)
                return Results.Forbid();

            return Results.Ok(await sender.Send(new ListDeveloperApiKeysQuery(userId.Value)));
        }).RequireAuthorization();

        management.MapPost("/keys", async (
            CreateDeveloperApiKeyRequest request,
            HttpContext http,
            ISender sender,
            IAdminAuthorization admin) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, http.RequestAborted) is not AdminRole.SuperAdmin)
                return Results.Forbid();

            var created = await sender.Send(new CreateDeveloperApiKeyCommand(userId.Value, request.Name));
            return Results.Created($"/api/developer/keys/{created.Id}", created);
        }).RequireAuthorization();

        management.MapDelete("/keys/{id:guid}", async (
            Guid id,
            HttpContext http,
            ISender sender,
            IAdminAuthorization admin) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, http.RequestAborted) is not AdminRole.SuperAdmin)
                return Results.Forbid();

            await sender.Send(new RevokeDeveloperApiKeyCommand(userId.Value, id));
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapGet("/v1/models", (IDeveloperAiGateway gateway) => Results.Ok(new
        {
            @object = "list",
            data = new[]
            {
                new { id = gateway.Model, @object = "model", owned_by = "englishai" },
                new { id = "free", @object = "model", owned_by = "englishai" },
            },
        })).AllowAnonymous();

        app.MapPost("/v1/chat/completions", ChatCompletionsAsync).AllowAnonymous();
        return app;
    }

    private static async Task ChatCompletionsAsync(
        HttpContext http,
        IDeveloperApiKeyStore keyStore,
        IDeveloperApiKeyProtector protector,
        IDeveloperAiGateway gateway,
        DeveloperAiOptions options,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (http.Request.ContentLength is > 0 && http.Request.ContentLength > options.MaxRequestBytes)
        {
            http.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await http.Response.WriteAsJsonAsync(
                new { error = new { code = "request_too_large", message = "Request body is too large." } },
                cancellationToken);
            return;
        }

        var apiKey = ReadBearer(http.Request.Headers.Authorization);
        var matched = await AuthenticateAsync(apiKey, keyStore, protector, cancellationToken);
        if (matched is null)
        {
            http.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await http.Response.WriteAsJsonAsync(
                new { error = new { code = "invalid_api_key", message = "Invalid or revoked API key." } },
                cancellationToken);
            return;
        }

        JsonDocument request;
        try
        {
            await using var limitedBody = new LimitedReadStream(http.Request.Body, options.MaxRequestBytes);
            request = await JsonDocument.ParseAsync(limitedBody, cancellationToken: cancellationToken);
        }
        catch (RequestBodyTooLargeException)
        {
            http.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await http.Response.WriteAsJsonAsync(
                new { error = new { code = "request_too_large", message = "Request body is too large." } },
                cancellationToken);
            return;
        }
        catch (JsonException)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await http.Response.WriteAsJsonAsync(
                new { error = new { code = "invalid_json", message = "Request body must be valid JSON." } },
                cancellationToken);
            return;
        }

        var concurrency = GetDeveloperConcurrency(options.MaxConcurrentRequests);

        if (!await concurrency.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            request.Dispose();
            http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            http.Response.Headers.RetryAfter = "5";
            await http.Response.WriteAsJsonAsync(
                new { error = new { code = "server_busy", message = "AI service is busy. Retry shortly." } },
                cancellationToken);
            return;
        }

        try
        {
            using (request)
            await using (var upstream = await gateway.SendChatCompletionAsync(request.RootElement, cancellationToken))
            {
                http.Response.StatusCode = (int)upstream.Response.StatusCode;
                if (upstream.Response.Content.Headers.ContentType is { } contentType)
                    http.Response.ContentType = contentType.ToString();

                foreach (var header in upstream.Response.Headers)
                {
                    if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                        continue;
                    http.Response.Headers[header.Key] = header.Value.ToArray();
                }

                if (upstream.Response.IsSuccessStatusCode)
                {
                    matched.RecordUsage(clock.GetUtcNow());
                    await keyStore.UpdateAsync(matched, cancellationToken);
                }

                await using var stream = await upstream.Response.Content.ReadAsStreamAsync(cancellationToken);
                await stream.CopyToAsync(http.Response.Body, cancellationToken);
            }
        }
        finally
        {
            concurrency.Release();
        }
    }

    private static async Task<DeveloperApiKey?> AuthenticateAsync(
        string? plaintext,
        IDeveloperApiKeyStore store,
        IDeveloperApiKeyProtector protector,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(plaintext) || !plaintext.StartsWith("eai_", StringComparison.Ordinal))
            return null;

        var prefix = plaintext[..Math.Min(DeveloperApiKey.PrefixLength, plaintext.Length)];
        var candidates = await store.GetActiveByPrefixAsync(prefix, cancellationToken);
        return candidates.FirstOrDefault(k => protector.Verify(plaintext, k.KeyHash));
    }

    private static string? ReadBearer(string? authorization)
    {
        const string scheme = "Bearer ";
        return authorization?.StartsWith(scheme, StringComparison.OrdinalIgnoreCase) == true
            ? authorization[scheme.Length..].Trim()
            : null;
    }

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public sealed record CreateDeveloperApiKeyRequest(string Name);

    private static SemaphoreSlim GetDeveloperConcurrency(int configuredLimit)
    {
        var limit = Math.Clamp(configuredLimit, 1, 32);
        if (DeveloperConcurrency is not null && DeveloperConcurrencyLimit == limit)
            return DeveloperConcurrency;

        lock (DeveloperConcurrencyLock)
        {
            if (DeveloperConcurrency is null || DeveloperConcurrencyLimit != limit)
            {
                DeveloperConcurrency = new SemaphoreSlim(limit, limit);
                DeveloperConcurrencyLimit = limit;
            }
            return DeveloperConcurrency;
        }
    }

    private sealed class RequestBodyTooLargeException : IOException;

    private sealed class LimitedReadStream(Stream inner, long maxBytes) : Stream
    {
        private long _read;
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => Track(inner.Read(buffer, offset, count));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Track(await inner.ReadAsync(buffer, cancellationToken));
        private int Track(int count)
        {
            _read += count;
            if (_read > maxBytes) throw new RequestBodyTooLargeException();
            return count;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { }
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
