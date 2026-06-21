namespace Application.Notifications.Ports;

/// <summary>
/// Durable per-learner "notifications read" watermark (see
/// <see cref="Domain.Notifications.NotificationReadState"/>). Because most notifications are
/// derived live from the learner's state rather than stored one row each, read-state is tracked as
/// a single timestamp: a notification is read once its <c>CreatedAt</c> is at or before the
/// watermark. Backed by EF Core/PostgreSQL in production, in-memory in dev/tests (docs/development-guide.md rule 10
/// - external services behind ports).
/// </summary>
public interface INotificationReadStateStore
{
    /// <summary>The learner's last "mark all read" moment, or <c>null</c> if they never have.</summary>
    Task<DateTimeOffset?> GetLastReadAtAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>Moves the learner's read watermark to the given moment (never backwards).</summary>
    Task SetLastReadAtAsync(Guid learnerId, DateTimeOffset lastReadAt, CancellationToken cancellationToken);
}
