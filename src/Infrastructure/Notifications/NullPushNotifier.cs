using Application.Notifications.Ports;

namespace Infrastructure.Notifications;

/// <summary>
/// No-op <see cref="IPushNotifier"/> wired when Firebase (FCM) is not configured. Keeps the app
/// running and testable without push credentials; in-app notifications (SignalR + the feed) are
/// unaffected - only the native status-bar push/badge is skipped.
/// </summary>
public sealed class NullPushNotifier : IPushNotifier
{
    public Task NotifyUsersAsync(
        IReadOnlyCollection<Guid> userAccountIds, PushMessage message, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task NotifyAllAsync(PushMessage message, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
