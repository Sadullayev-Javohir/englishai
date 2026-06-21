using System.Collections.Concurrent;
using Application.Notifications.Ports;
using Domain.Notifications;

namespace Infrastructure.Notifications;

/// <summary>
/// Dev/in-app <see cref="INotificationDispatcher"/>: records delivered notifications in
/// memory so the Notifications screen can read them back. A real push/SignalR adapter
/// can replace this behind the same port without touching the Application layer
/// (docs/development-guide.md rule 10). State is process-local and resets on restart - acceptable until
/// durable delivery is wired in a later phase.
/// </summary>
public sealed class InMemoryNotificationDispatcher : INotificationDispatcher
{
    private readonly ConcurrentDictionary<Guid, List<Notification>> _byLearner = new();

    public Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        var list = _byLearner.GetOrAdd(notification.LearnerId, _ => new List<Notification>());
        lock (list)
        {
            list.Add(notification);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Notification>> GetForLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        if (!_byLearner.TryGetValue(learnerId, out var list))
            return Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());

        lock (list)
        {
            IReadOnlyList<Notification> snapshot = list
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            return Task.FromResult(snapshot);
        }
    }

    public Task MarkAllReadAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        if (_byLearner.TryGetValue(learnerId, out var list))
        {
            lock (list)
            {
                foreach (var notification in list)
                    notification.MarkRead();
            }
        }

        return Task.CompletedTask;
    }
}
