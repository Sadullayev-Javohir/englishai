using System.Collections.Concurrent;
using Application.Notifications.Ports;

namespace Infrastructure.Notifications;

/// <summary>
/// Process-local <see cref="INotificationDismissalStore"/> for the no-database dev path. Dismissals
/// reset on restart - acceptable without a database; the EF adapter provides durable dismissals in
/// real deployments.
/// </summary>
public sealed class InMemoryNotificationDismissalStore : INotificationDismissalStore
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, DateTimeOffset>> _byLearner = new();

    public Task<IReadOnlyCollection<Guid>> GetDismissedAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        var set = _byLearner.TryGetValue(learnerId, out var ids)
            ? (IReadOnlyCollection<Guid>)ids.Keys.ToArray()
            : Array.Empty<Guid>();
        return Task.FromResult(set);
    }

    public Task DismissAsync(
        Guid learnerId, Guid notificationId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var set = _byLearner.GetOrAdd(learnerId, _ => new ConcurrentDictionary<Guid, DateTimeOffset>());
        set.TryAdd(notificationId, now);
        return Task.CompletedTask;
    }
}
