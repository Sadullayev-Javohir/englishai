using Domain.Notifications;

namespace Application.Notifications.Ports;

/// <summary>
/// Durable store for super-admin broadcasts. One row per broadcast, shown to every learner's feed and
/// gated by the shared read watermark (docs/development-guide.md rule 10 - persistence behind a port).
/// </summary>
public interface IAdminBroadcastStore
{
    /// <summary>Persists a newly composed broadcast.</summary>
    Task AddAsync(AdminBroadcast broadcast, CancellationToken cancellationToken);

    /// <summary>Broadcasts created at or after <paramref name="since"/>, newest first.</summary>
    Task<IReadOnlyList<AdminBroadcast>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>The most recent broadcasts (for the admin console), newest first.</summary>
    Task<IReadOnlyList<AdminBroadcast>> GetRecentAsync(
        DateTimeOffset? beforeCreatedAt, Guid? beforeId, int limit, CancellationToken cancellationToken);

    /// <summary>Removes one broadcast by id (super-admin dismiss). Returns false if it no longer exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
