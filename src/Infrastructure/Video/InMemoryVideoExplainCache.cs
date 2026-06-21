using System.Collections.Concurrent;
using Application.Video.Ports;

namespace Infrastructure.Video;

public sealed class InMemoryVideoExplainCache : IVideoExplainCache
{
    private const int MaxEntries = 5000;
    private static readonly TimeSpan TimeToLive = TimeSpan.FromHours(24);
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            return Task.FromResult<string?>(entry.Reply);

        _entries.TryRemove(key, out _);
        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(string key, string reply, CancellationToken cancellationToken = default)
    {
        if (_entries.Count >= MaxEntries && !_entries.ContainsKey(key))
        {
            var victim = _entries.OrderBy(pair => pair.Value.ExpiresAt).FirstOrDefault();
            if (!string.IsNullOrEmpty(victim.Key))
                _entries.TryRemove(victim.Key, out _);
        }

        _entries[key] = new Entry(reply, DateTimeOffset.UtcNow + TimeToLive);
        return Task.CompletedTask;
    }

    private sealed record Entry(string Reply, DateTimeOffset ExpiresAt);
}
