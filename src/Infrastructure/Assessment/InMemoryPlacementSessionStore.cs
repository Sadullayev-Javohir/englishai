using System.Runtime.CompilerServices;
using Application.Assessment.Ports;
using Application.Common;
using Domain.Assessment;

namespace Infrastructure.Assessment;

/// <summary>
/// Local/test session store with snapshot isolation and optimistic concurrency,
/// mirroring Redis semantics. Registered as a singleton across requests.
/// </summary>
public sealed class InMemoryPlacementSessionStore : IPlacementSessionStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Entry> _sessions = new();
    private readonly ConditionalWeakTable<PlacementTestSession, Entry> _versions = new();
    private readonly TimeProvider _clock;
    private sealed record Entry(PlacementSessionSnapshot Snapshot, DateTimeOffset ExpiresAt);

    public InMemoryPlacementSessionStore(TimeProvider? clock = null) => _clock = clock ?? TimeProvider.System;

    public Task<PlacementTestSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var entry) || entry.ExpiresAt <= _clock.GetUtcNow())
                return Task.FromResult<PlacementTestSession?>(null);
            var session = PlacementTestSession.Restore(entry.Snapshot);
            _versions.Add(session, entry);
            return Task.FromResult<PlacementTestSession?>(session);
        }
    }

    public Task SaveAsync(PlacementTestSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _sessions.TryGetValue(session.Id, out var current);
            _versions.TryGetValue(session, out var expected);
            if (!ReferenceEquals(current, expected))
                throw new ConflictException("The placement session changed. Resume it before submitting again.");
            var entry = new Entry(session.ToSnapshot(), _clock.GetUtcNow().AddHours(3));
            _sessions[session.Id] = entry;
            _versions.Remove(session);
            _versions.Add(session, entry);
        }
        return Task.CompletedTask;
    }
}
