using Application.Video.Models;
using Application.Video.Ports;
using FluentAssertions;
using Infrastructure.Video;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the feed cache that lets the adaptive feed scale to many learners on a tiny free quota:
/// identical pages are served from one upstream fetch, the entry expires after the TTL, and a
/// transient empty page is never cached (so the next request retries - docs/development-guide.md rules 8, 10).
/// </summary>
public class CachingVideoFeedSourceTests
{
    private sealed class CountingFeedSource : IVideoFeedSource
    {
        public int Calls { get; private set; }
        public VideoFeedPage Next { get; set; } =
            new(new[] { new VideoFeedResult("id1", "Title", "Channel", 100) }, "tok");

        public Task<VideoFeedPage> SearchAsync(
            string searchTerm, string? continuation, int pageSize, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Next);
        }
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public void Advance(TimeSpan by) => _now += by;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static (CachingVideoFeedSource cache, CountingFeedSource inner, MutableClock clock) Build(TimeSpan ttl)
    {
        var inner = new CountingFeedSource();
        var provider = new ServiceCollection().AddSingleton(inner).BuildServiceProvider();
        var clock = new MutableClock(DateTimeOffset.UnixEpoch);
        var cache = new CachingVideoFeedSource(
            provider.GetRequiredService<IServiceScopeFactory>(),
            p => p.GetRequiredService<CountingFeedSource>(),
            clock,
            ttl);
        return (cache, inner, clock);
    }

    [Fact]
    public async Task Identical_requests_within_ttl_hit_the_upstream_once()
    {
        var (cache, inner, _) = Build(TimeSpan.FromHours(6));

        var first = await cache.SearchAsync("b1 listening", null, 12);
        var second = await cache.SearchAsync("b1 listening", null, 12);

        inner.Calls.Should().Be(1, "the second identical request is served from cache");
        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task Different_pages_are_cached_independently()
    {
        var (cache, inner, _) = Build(TimeSpan.FromHours(6));

        await cache.SearchAsync("b1 listening", null, 12);   // first page
        await cache.SearchAsync("b1 listening", "tok", 12);   // continuation page
        await cache.SearchAsync("a2 listening", null, 12);    // different level

        inner.Calls.Should().Be(3);
    }

    [Fact]
    public async Task A_request_after_the_ttl_refetches()
    {
        var (cache, inner, clock) = Build(TimeSpan.FromHours(6));

        await cache.SearchAsync("b1 listening", null, 12);
        clock.Advance(TimeSpan.FromHours(6) + TimeSpan.FromMinutes(1));
        await cache.SearchAsync("b1 listening", null, 12);

        inner.Calls.Should().Be(2, "the cached entry expired so the upstream is hit again");
    }

    [Fact]
    public async Task An_empty_page_is_not_cached()
    {
        var (cache, inner, _) = Build(TimeSpan.FromHours(6));
        inner.Next = new VideoFeedPage(Array.Empty<VideoFeedResult>(), null);

        await cache.SearchAsync("b1 listening", null, 12);
        await cache.SearchAsync("b1 listening", null, 12);

        inner.Calls.Should().Be(2, "a transient empty result must not be memoised for the whole TTL");
    }
}
