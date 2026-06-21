using Application.Notifications.Dtos;
using Application.Notifications.Ports;
using Microsoft.AspNetCore.SignalR;
using Web.Hubs;
using Web.Observability;

namespace Web.Notifications;

/// <summary>
/// SignalR adapter for <see cref="INotificationRealtimeNotifier"/>: delivers a freshly sent broadcast
/// to every connected learner over the <see cref="NotificationsHub"/>. Best-effort - the broadcast is
/// persisted independently, so an offline learner still receives it on their next fetch.
/// </summary>
public sealed class SignalRNotificationNotifier : INotificationRealtimeNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly ILogger<SignalRNotificationNotifier> _logger;

    public SignalRNotificationNotifier(
        IHubContext<NotificationsHub> hub,
        ILogger<SignalRNotificationNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task BroadcastSentAsync(NotificationDto notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients.All.SendAsync(
                NotificationsHub.BroadcastReceivedEvent, notification, cancellationToken);
            EnglishAiTelemetry.SignalRSendEvents.Add(
                1,
                new("hub", "notifications"),
                new("target", "global"),
                new("outcome", "sent"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            EnglishAiTelemetry.SignalRSendEvents.Add(
                1,
                new("hub", "notifications"),
                new("target", "global"),
                new("outcome", "failed"));
            _logger.LogWarning(
                exception,
                "Persisted notification broadcast {NotificationId} could not be delivered in real time.",
                notification.Id);
        }
    }

    public async Task NotificationSentAsync(
        Guid recipientId, NotificationDto notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients.Group(NotificationsHub.UserGroup(recipientId)).SendAsync(
                NotificationsHub.NotificationReceivedEvent, notification, cancellationToken);
            EnglishAiTelemetry.SignalRSendEvents.Add(
                1,
                new("hub", "notifications"),
                new("target", "user"),
                new("outcome", "sent"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            EnglishAiTelemetry.SignalRSendEvents.Add(
                1,
                new("hub", "notifications"),
                new("target", "user"),
                new("outcome", "failed"));
            _logger.LogWarning(exception,
                "Persisted notification {NotificationId} for account {RecipientId} could not be delivered in real time.",
                notification.Id, recipientId);
        }
    }
}
