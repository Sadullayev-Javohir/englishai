using System.Collections.Concurrent;

namespace Infrastructure.Speaking;

/// <summary>
/// Single-process <see cref="IVoiceLiveSessionStore"/> used when no Redis connection is
/// configured (local development and tests). Accounting is per-instance and lost on restart,
/// which is exactly why production wires the Redis implementation.
/// </summary>
public sealed class InMemoryVoiceLiveSessionStore : IVoiceLiveSessionStore
{
    private readonly ConcurrentDictionary<string, Entry> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, double> _minutes = new(StringComparer.Ordinal);
    private readonly TimeProvider _clock;

    public InMemoryVoiceLiveSessionStore(TimeProvider clock)
    {
        _clock = clock;
    }

    public Task CreateAsync(VoiceLiveSessionRecord record, TimeSpan ttl, CancellationToken cancellationToken)
    {
        _sessions[record.SessionId] = new Entry(record, _clock.GetUtcNow() + ttl);
        return Task.CompletedTask;
    }

    public Task<VoiceLiveSessionRecord?> TakeAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!_sessions.TryRemove(sessionId, out var entry))
            return Task.FromResult<VoiceLiveSessionRecord?>(null);

        // Mirror the Redis TTL: an expired reservation is gone, not settleable.
        return Task.FromResult(entry.ExpiresAt <= _clock.GetUtcNow() ? null : entry.Record);
    }

    public Task<double> AddDailyMinutesAsync(
        string callerKey,
        DateOnly day,
        double deltaMinutes,
        CancellationToken cancellationToken)
    {
        var total = _minutes.AddOrUpdate(
            MinutesKey(callerKey, day),
            _ => Math.Max(0, deltaMinutes),
            (_, current) => Math.Max(0, current + deltaMinutes));
        return Task.FromResult(total);
    }

    public Task<double> GetDailyMinutesAsync(string callerKey, DateOnly day, CancellationToken cancellationToken) =>
        Task.FromResult(_minutes.TryGetValue(MinutesKey(callerKey, day), out var minutes) ? minutes : 0);

    private static string MinutesKey(string callerKey, DateOnly day) => $"{day:yyyy-MM-dd}:{callerKey}";

    private sealed record Entry(VoiceLiveSessionRecord Record, DateTimeOffset ExpiresAt);
}
