using Application.Notifications.Ports;
using Domain.Notifications;

namespace Infrastructure.Notifications;

/// <summary>
/// Process-local <see cref="IAdminBroadcastStore"/> for the no-database dev path. State resets on
/// restart - acceptable when no PostgreSQL is configured; the EF adapter is used in real deployments.
/// A plain list behind a lock (not a <see cref="System.Collections.Concurrent.ConcurrentBag{T}"/>) so
/// individual broadcasts can be removed.
/// </summary>
public sealed class InMemoryAdminBroadcastStore : IAdminBroadcastStore
{
    private readonly List<AdminBroadcast> _broadcasts = new();
    private readonly object _gate = new();

    public Task AddAsync(AdminBroadcast broadcast, CancellationToken cancellationToken)
    {
        lock (_gate)
            _broadcasts.Add(broadcast);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AdminBroadcast>> GetSinceAsync(
        DateTimeOffset since, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<AdminBroadcast> result = _broadcasts
                .Where(b => b.CreatedAt >= since)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<AdminBroadcast>> GetRecentAsync(
        DateTimeOffset? beforeCreatedAt, Guid? beforeId, int limit, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<AdminBroadcast> result = _broadcasts
            .Where(b => !beforeCreatedAt.HasValue || !beforeId.HasValue
                || b.CreatedAt < beforeCreatedAt.Value
                || b.CreatedAt == beforeCreatedAt.Value && b.Id.CompareTo(beforeId.Value) < 0)
            .OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
                .Take(limit)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
            return Task.FromResult(_broadcasts.RemoveAll(b => b.Id == id) > 0);
    }
}
