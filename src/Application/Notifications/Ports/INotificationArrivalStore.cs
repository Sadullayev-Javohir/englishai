using Domain.Notifications;

namespace Application.Notifications.Ports;

/// <summary>
/// Records and reads the durable first-seen timestamp of a derived reminder occurrence so it shows a
/// real arrival time instead of a synthetic one, stable across fetches and restarts.
/// </summary>
public interface INotificationArrivalStore
{
    /// <summary>
    /// Returns the stored first-seen time for each requested key, creating a row stamped with
    /// <paramref name="now"/> for any key not seen before. Idempotent: the first call for a key fixes
    /// its timestamp; later calls return that same value.
    /// </summary>
    Task<IReadOnlyDictionary<string, DateTimeOffset>> GetOrCreateAsync(
        IReadOnlyCollection<string> keys, DateTimeOffset now, CancellationToken cancellationToken);
}
