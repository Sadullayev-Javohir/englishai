using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Web;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Guards the two pieces of custom logic behind the perimeter rate limiter: which requests are
/// exempt from throttling (breaking these would break real-time/media in prod), and how a caller
/// is bucketed (a mix-up would let one user's traffic count against another). Pure and
/// host-free, so no app boot / config-layering races.
/// </summary>
public sealed class RateLimitPolicyTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/api/images/topics/abc")]
    [InlineData("/api/listening/topic/abc/audio")]
    [InlineData("/api/placement/audio/abc")]
    public void Exempt_paths_are_never_throttled(string path)
    {
        RateLimitPolicy.IsExempt(new PathString(path)).Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/speaking/utterance")]
    [InlineData("/api/vocabulary/topics")]
    [InlineData("/api/auth/google")]
    public void Normal_endpoints_are_subject_to_the_limit(string path)
    {
        RateLimitPolicy.IsExempt(new PathString(path)).Should().BeFalse();
    }

    [Theory]
    [InlineData("/api/auth/google", true)]
    [InlineData("/api/auth/logout", true)]
    [InlineData("/v1/chat/completions", false)]
    [InlineData("/api/speaking/utterance", false)]
    public void Auth_surface_is_recognised_for_the_tighter_window(string path, bool expected)
    {
        RateLimitPolicy.IsAuthSurface(new PathString(path)).Should().Be(expected);
    }

    [Theory]
    [InlineData("/v1/chat/completions", true)]
    [InlineData("/v1/chat/completions/", true)]
    [InlineData("/v1/models", false)]
    [InlineData("/api/auth/google", false)]
    public void Developer_API_surface_is_recognised(string path, bool expected)
    {
        RateLimitPolicy.IsDeveloperApiSurface(new PathString(path)).Should().Be(expected);
    }

    [Fact]
    public void Developer_API_callers_bucket_by_non_secret_key_prefix()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer eai_1234567890_super_secret_value";

        RateLimitPolicy.ResolveDeveloperApiKey(context).Should().Be("key:eai_12345678");
    }

    [Fact]
    public void Invalid_developer_API_credentials_fall_back_to_client_IP()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        context.Request.Headers.Authorization = "Bearer invalid";

        RateLimitPolicy.ResolveDeveloperApiKey(context).Should().Be("ip:203.0.113.7");
    }

    [Fact]
    public void Authenticated_callers_bucket_by_their_own_user_id()
    {
        const string sub = "11111111-1111-1111-1111-111111111111";
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", sub) })),
        };
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        // The user id wins over the IP, so two sessions from behind one NAT don't share a bucket.
        RateLimitPolicy.ResolveClientKey(context).Should().Be($"u:{sub}");
    }

    [Fact]
    public void Anonymous_callers_bucket_by_client_ip()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        RateLimitPolicy.ResolveClientKey(context).Should().Be("ip:203.0.113.7");
    }
}
