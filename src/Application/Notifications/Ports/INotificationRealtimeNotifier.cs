using Application.Notifications.Dtos;

namespace Application.Notifications.Ports;

/// <summary>
/// Pushes a freshly sent notification to connected learners in real time, so it appears in their feed
/// (and the bell badge) the moment it is sent - without waiting for a page refresh or the next poll.
/// A port (docs/development-guide.md §4): the Application layer raises the event; an adapter in the Web layer delivers
/// it over the real-time transport (SignalR). Delivery is best-effort - a learner who is offline still
/// sees the broadcast on their next fetch, since it is persisted independently.
/// </summary>
public interface INotificationRealtimeNotifier
{
    /// <summary>Notifies every connected learner that a super-admin broadcast has just been sent.</summary>
    Task BroadcastSentAsync(NotificationDto notification, CancellationToken cancellationToken);

    /// <summary>Notifies one connected account that its persisted feed has a new item.</summary>
    Task NotificationSentAsync(Guid recipientId, NotificationDto notification, CancellationToken cancellationToken);
}
