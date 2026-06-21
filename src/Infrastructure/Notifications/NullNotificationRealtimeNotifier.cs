using Application.Notifications.Dtos;
using Application.Notifications.Ports;

namespace Infrastructure.Notifications;

public sealed class NullNotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    public Task BroadcastSentAsync(NotificationDto notification, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task NotificationSentAsync(Guid recipientId, NotificationDto notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
