using Domain.Notifications;

namespace Application.Notifications.Ports;

/// <summary>
/// Delivery port for learner notifications. The dev/in-app adapter records them for the
/// in-app feed; a real push/SignalR adapter can implement the same port later without
/// touching the Application layer (docs/development-guide.md rule 10 - external services behind ports).
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>Delivers a notification to its learner.</summary>
    Task SendAsync(Notification notification, CancellationToken cancellationToken);

    /// <summary>Returns a learner's notifications, newest first.</summary>
    Task<IReadOnlyList<Notification>> GetForLearnerAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>Marks all of a learner's notifications as read.</summary>
    Task MarkAllReadAsync(Guid learnerId, CancellationToken cancellationToken);
}
