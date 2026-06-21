namespace Application.Notifications.Ports;

/// <summary>
/// Durable per-notification dismissal set. Complements the single "mark all read" watermark
/// (<see cref="INotificationReadStateStore"/>): tapping one notification records its id here so that
/// exact occurrence drops out of the feed while everything else stays. Backed by EF Core/PostgreSQL in
/// production and in-memory in dev/tests (docs/development-guide.md rule 10 - external services behind ports).
/// </summary>
public interface INotificationDismissalStore
{
    /// <summary>The ids of every notification the learner has individually dismissed.</summary>
    Task<IReadOnlyCollection<Guid>> GetDismissedAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>Records that the learner dismissed the given notification. Idempotent.</summary>
    Task DismissAsync(
        Guid learnerId, Guid notificationId, DateTimeOffset now, CancellationToken cancellationToken);
}
