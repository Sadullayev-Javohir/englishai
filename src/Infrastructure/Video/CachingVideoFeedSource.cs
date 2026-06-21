using System.Collections.Concurrent;
using Application.Video.Models;
using Application.Video.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Video;

/// <summary>
/// An <see cref="IVideoFeedSource"/> decorator that caches each feed page in memory for a TTL, so the
/// adaptive feed scales to many concurrent learners without exhausting the underlying source's budget
/// (PROJECT-SPEC B.3, docs/development-guide.md rule 10). It is the difference between free-forever and quota-blown at
/// scale: learners at the same CEFR level all request the same level-keyed first page (and the same
/// deterministic continuation pages as they scroll), so one cached fetch serves thousands of users.
/// The free YouTube Data API allows ~100 searches/day; with a multi-hour TTL the handful of distinct
/// (level × page) searches stay far inside that, so 10k+ users cost the same as one.
///
/// Resilience (rule 8): only non-empty pages are cached - a transient empty/failed fetch is never
/// memoised, so the next request retries rather than serving an empty feed for the whole TTL. The
/// inner source is resolved per call from a fresh DI scope so its pooled <see cref="HttpClient"/> is
/// managed by the factory (no captured-client staleness, no transient-disposable leak from the root).
/// </summary>
public sealed class CachingVideoFeedSource : IVideoFeedSource
{
    /// <summary>Hard cap on cached entries; a prune drops expired ones first, then the oldest.</summary>
    private const int MaxEntries = 512;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Func<IServiceProvider, IVideoFeedSource> _resolveInner;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _ttl;
    private readonly ConcurrentDictionary<string, Entry> _cache = new();

    public CachingVideoFeedSource(
        IServiceScopeFactory scopeFactory,
        Func<IServiceProvider, IVideoFeedSource> resolveInner,
        TimeProvider clock,
        TimeSpan ttl)
    {
        _scopeFactory = scopeFactory;
        _resolveInner = resolveInner;
        _clock = clock;
        _ttl = ttl;
    }

    public async Task<VideoFeedPage> SearchAsync(
        string searchTerm, string? continuation, int pageSize, CancellationToken cancellationToken = default)
    {
        var key = $"{searchTerm}{continuation ?? ""}{pageSize}";
        var now = _clock.GetUtcNow();

        if (_cache.TryGetValue(key, out var hit) && hit.ExpiresAt > now)
            return hit.Page;

        using var scope = _scopeFactory.CreateScope();
        var inner = _resolveInner(scope.ServiceProvider);
        var page = await inner.SearchAsync(searchTerm, continuation, pageSize, cancellationToken);

        // Only memoise a real, non-empty page: caching an empty result would serve a transient block
        // (or a momentary upstream hiccup) to everyone for the whole TTL instead of retrying (rule 8).
        if (page.Items.Count > 0)
        {
            _cache[key] = new Entry(page, now + _ttl);
            if (_cache.Count > MaxEntries)
                Prune(now);
        }

        return page;
    }

    /// <summary>Drops expired entries; if still over the cap, drops the soonest-to-expire ones.</summary>
    private void Prune(DateTimeOffset now)
    {
        foreach (var (k, v) in _cache)
            if (v.ExpiresAt <= now)
                _cache.TryRemove(k, out _);

        if (_cache.Count <= MaxEntries)
            return;

        foreach (var k in _cache.OrderBy(e => e.Value.ExpiresAt).Take(_cache.Count - MaxEntries).Select(e => e.Key).ToList())
            _cache.TryRemove(k, out _);
    }

    private readonly record struct Entry(VideoFeedPage Page, DateTimeOffset ExpiresAt);
}
