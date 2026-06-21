using Domain.Notifications;

namespace Application.Notifications.Ports;

/// <summary>
/// Store of learners' push registration tokens. Durable when a database is configured so a device
/// stays reachable across restarts; in-memory otherwise. Registration is an upsert keyed by the token
/// itself (a device re-registering must not create duplicates).
/// </summary>
public interface IDeviceTokenStore
{
    /// <summary>Registers or refreshes a device token for a learner.</summary>
    Task UpsertAsync(Guid userAccountId, string token, string platform, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Returns every registration token for the given learners.</summary>
    Task<IReadOnlyList<DeviceToken>> GetForUsersAsync(IReadOnlyCollection<Guid> userAccountIds, CancellationToken cancellationToken);

    /// <summary>Returns every registration token across all learners (for broadcasts).</summary>
    Task<IReadOnlyList<DeviceToken>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Removes tokens FCM reported as invalid/unregistered so the set stays reachable.</summary>
    Task RemoveAsync(IReadOnlyCollection<string> tokens, CancellationToken cancellationToken);
}
