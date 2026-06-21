using System.Net;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Observability;
using Web.RateLimiting;
using Xunit;

namespace Integration.Tests;

public sealed class DistributedRateLimitTests
{
    [Fact]
    public void Default_ai_limits_allow_normal_use_without_removing_abuse_protection()
    {
        var options = new DistributedRateLimitOptions();

        options.AiPermitPerMinute.Should().Be(40);
        options.TranslationPermitPerMinute.Should().Be(30);
        options.GlobalEmergencyPermitPerMinute.Should().Be(10000);
    }

    [Fact]
    public async Task Two_app_instances_share_the_same_limit()
    {
        var store = new AtomicTestStore();
        var options = Options(permit: 2);
        var first = Middleware(store, options);
        var second = Middleware(store, options);

        (await InvokeAsync(first)).StatusCode.Should().Be(200);
        (await InvokeAsync(second)).StatusCode.Should().Be(200);
        var rejected = await InvokeAsync(first);
        rejected.StatusCode.Should().Be(429);
        var payload = JsonSerializer.Deserialize<RateLimitErrorResponse>(rejected.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        })!;
        payload.Code.Should().Be("rate_limited");
        payload.RetryAfterSeconds.Should().BeGreaterThan(0);
        payload.CorrelationId.Should().Be("corr-test");
    }

    [Fact]
    public async Task Redis_failure_fails_closed_for_auth_and_allows_bounded_core_reads()
    {
        var options = Options(permit: 10);
        options.CoreReadFallbackPermitPerMinute = 1;
        var middleware = Middleware(new FailingStore(), options);

        var auth = await InvokeAsync(middleware, "/api/auth/config");
        auth.StatusCode.Should().Be(503);
        (await InvokeAsync(middleware, "/api/public/metrics")).StatusCode.Should().Be(200);
        (await InvokeAsync(middleware, "/api/public/metrics")).StatusCode.Should().Be(503);
    }

    [Fact]
    public void Developer_keys_are_hashed_and_never_exposed()
    {
        var context = Context("/v1/chat/completions");
        context.Request.Headers.Authorization = "Bearer eai_super_secret";
        var key = DistributedRateLimitMiddleware.DeveloperKey(context);
        key.Should().StartWith("key:").And.NotContain("eai_super_secret");
    }

    [Theory]
    [InlineData("GET", "/api/speaking/practice-words/2e53b703-8cbc-4b40-afdb-b1ca0254965f")]
    [InlineData("GET", "/api/speaking/word/example")]
    [InlineData("GET", "/api/speaking/free-talk-topics")]
    [InlineData("GET", "/api/speaking/roleplay/scenarios")]
    [InlineData("GET", "/api/writing/catalog/2e53b703-8cbc-4b40-afdb-b1ca0254965f")]
    [InlineData("GET", "/api/assistant/sessions")]
    [InlineData("POST", "/api/assistant/sessions")]
    [InlineData("GET", "/api/translation")]
    public void Non_generating_endpoints_use_the_core_policy(string method, string path)
    {
        DistributedRateLimitMiddleware.IsAiSurface(Request(method, path)).Should().BeFalse();
    }

    [Theory]
    [InlineData("POST", "/api/video/explain")]
    [InlineData("POST", "/api/video/explain/stream")]
    [InlineData("POST", "/api/translate")]
    [InlineData("POST", "/api/assistant/ask")]
    [InlineData("POST", "/api/assistant/project")]
    [InlineData("POST", "/api/assistant/context/stream")]
    [InlineData("POST", "/api/assistant/sessions/2e53b703-8cbc-4b40-afdb-b1ca0254965f/messages/stream")]
    [InlineData("GET", "/api/writing/topic/2e53b703-8cbc-4b40-afdb-b1ca0254965f")]
    [InlineData("POST", "/api/writing/submit")]
    [InlineData("POST", "/api/speaking/start")]
    [InlineData("POST", "/api/speaking/utterance/stream")]
    [InlineData("POST", "/api/speaking/practice-words/2e53b703-8cbc-4b40-afdb-b1ca0254965f/attempt")]
    [InlineData("POST", "/api/speaking/segment-pronunciation")]
    [InlineData("POST", "/api/speaking/idea-cards")]
    [InlineData("POST", "/api/speaking/roleplay/start")]
    [InlineData("POST", "/api/speaking/roleplay/evaluate")]
    public void Generating_endpoints_use_the_ai_policy(string method, string path)
    {
        DistributedRateLimitMiddleware.IsAiSurface(Request(method, path)).Should().BeTrue();
    }

    [Fact]
    public async Task Fixed_window_keeps_the_original_ttl_and_resets_after_expiry()
    {
        var store = new AtomicTestStore();
        var first = await store.AcquireAsync("ttl", 1, TimeSpan.FromSeconds(60), default);
        var second = await store.AcquireAsync("ttl", 1, TimeSpan.FromSeconds(60), default);
        first.RetryAfterSeconds.Should().Be(60);
        second.RetryAfterSeconds.Should().Be(60);
        second.Allowed.Should().BeFalse();

        store.Expire("ttl");
        (await store.AcquireAsync("ttl", 1, TimeSpan.FromSeconds(60), default)).Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task Translation_is_not_rejected_by_distributed_rate_limits()
    {
        var store = new AtomicTestStore();
        var options = Options(permit: 1);
        options.TranslationPermitPerMinute = 2;
        var middleware = Middleware(store, options);

        (await InvokeAsync(middleware, "/api/speaking/start", HttpMethods.Post)).StatusCode.Should().Be(200);
        (await InvokeAsync(middleware, "/api/translate", HttpMethods.Post)).StatusCode.Should().Be(200);
        (await InvokeAsync(middleware, "/api/translate", HttpMethods.Post)).StatusCode.Should().Be(200);
    }

    private static DistributedRateLimitMiddleware Middleware(IDistributedRateLimitStore store, DistributedRateLimitOptions options) =>
        new(_ => Task.CompletedTask, store, options, NullLogger<DistributedRateLimitMiddleware>.Instance);

    private static DistributedRateLimitOptions Options(int permit) => new()
    {
        Enabled = true,
        PermitPerMinute = permit,
        AuthPermitPerMinute = permit,
        DeveloperPermitPerMinute = permit,
        AiPermitPerMinute = permit,
        TranslationPermitPerMinute = permit,
        GlobalEmergencyPermitPerMinute = 100,
        RedisTimeoutMilliseconds = 1000,
    };

    private static async Task<ResponseSnapshot> InvokeAsync(DistributedRateLimitMiddleware middleware, string path = "/api/public/metrics", string method = "GET")
    {
        var context = Context(path);
        context.Request.Method = method;
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return new ResponseSnapshot(context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private static DefaultHttpContext Context(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = HttpMethods.Get;
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.Items[CorrelationConstants.ItemKey] = "corr-test";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static HttpRequest Request(string method, string path)
    {
        var context = Context(path);
        context.Request.Method = method;
        return context.Request;
    }

    private sealed record ResponseSnapshot(int StatusCode, string Body);

    private sealed class AtomicTestStore : IDistributedRateLimitStore
    {
        private readonly Dictionary<string, long> _counts = new();
        public Task<RateLimitLease> AcquireAsync(string key, int permitLimit, TimeSpan window, CancellationToken cancellationToken)
        {
            lock (_counts)
            {
                var count = _counts.GetValueOrDefault(key) + 1;
                _counts[key] = count;
                return Task.FromResult(new RateLimitLease(count <= permitLimit, 60, count));
            }
        }

        public void Expire(string key)
        {
            lock (_counts) _counts.Remove(key);
        }
    }

    private sealed class FailingStore : IDistributedRateLimitStore
    {
        public Task<RateLimitLease> AcquireAsync(string key, int permitLimit, TimeSpan window, CancellationToken cancellationToken) =>
            throw new TimeoutException("redis unavailable");
    }
}
