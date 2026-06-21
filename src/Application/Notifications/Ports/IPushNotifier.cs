namespace Application.Notifications.Ports;

/// <summary>
/// The content of a native push: a status-bar title/body plus an optional deep-link the app opens
/// when the notification is tapped. Uzbek text is supplied by the caller from vetted templates
/// (docs/development-guide.md rule 11) - this port never generates copy.
/// </summary>
public sealed record PushMessage(string Title, string Body, string? LinkUrl = null);

/// <summary>
/// Delivers native push notifications (status-bar + app badge) to learners' registered devices,
/// so a broadcast or daily reminder reaches them even when the app is closed. A port (docs/development-guide.md §4):
/// the Application layer raises the intent; an adapter in Infrastructure talks to FCM. Delivery is
/// best-effort and self-contained - when push is not configured, a no-op adapter is wired so the app
/// runs and is testable without Firebase credentials (mirrors the Local*/Null* stand-ins).
/// </summary>
public interface IPushNotifier
{
    /// <summary>Pushes a message to every registered device of the given learners.</summary>
    Task NotifyUsersAsync(IReadOnlyCollection<Guid> userAccountIds, PushMessage message, CancellationToken cancellationToken);

    /// <summary>Pushes a message to every registered device across all learners (super-admin broadcast).</summary>
    Task NotifyAllAsync(PushMessage message, CancellationToken cancellationToken);
}
