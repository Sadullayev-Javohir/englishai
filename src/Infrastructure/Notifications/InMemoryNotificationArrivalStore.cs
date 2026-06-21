using System.Collections.Concurrent;
using Application.Notifications.Ports;

namespace Infrastructure.Notifications;

/// <summary>
/// Process-local <see cref="INotificationArrivalStore"/> for the no-database dev path. First-seen
/// times reset on restart (reminders reappear as unread after a restart) - acceptable without a
/// database; the EF adapter provides durable arrival times in real deployments.
/// </summary>
public sealed class InMemoryNotificationArrivalStore : INotificationArrivalStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _firstSeen = new();

    public Task<IReadOnlyDictionary<string, DateTimeOffset>> GetOrCreateAsync(
        IReadOnlyCollection<string> keys, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        foreach (var key in keys)
            result[key] = _firstSeen.GetOrAdd(key, now);

        return Task.FromResult<IReadOnlyDictionary<string, DateTimeOffset>>(result);
    }
}
