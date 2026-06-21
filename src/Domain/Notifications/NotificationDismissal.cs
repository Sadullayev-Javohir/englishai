using Domain.Common;

namespace Domain.Notifications;

/// <summary>
/// Records that a learner dismissed one specific notification by tapping it (PROJECT-SPEC Qism A).
/// Unlike the single "mark all read" watermark (<see cref="NotificationReadState"/>), this dismisses
/// exactly one feed item: the moment a learner taps a notification it opens its destination and drops
/// out of the feed, staying gone for that occurrence. Because the derived reminders (SRS by topic, the
/// daily nudge) carry a day-scoped id, dismissing today's occurrence hides only today's - a genuinely
/// new occurrence tomorrow reappears.
/// </summary>
public sealed class NotificationDismissal
{
    // Parameterless ctor for EF Core materialization.
    private NotificationDismissal()
    {
    }

    private NotificationDismissal(Guid learnerId, Guid notificationId, DateTimeOffset dismissedAt)
    {
        LearnerId = learnerId;
        NotificationId = notificationId;
        DismissedAt = dismissedAt;
    }

    /// <summary>The learner who dismissed the notification.</summary>
    public Guid LearnerId { get; private set; }

    /// <summary>The dismissed notification's id (stable per occurrence - see the feed builder).</summary>
    public Guid NotificationId { get; private set; }

    /// <summary>When it was dismissed.</summary>
    public DateTimeOffset DismissedAt { get; private set; }

    public static NotificationDismissal Create(Guid learnerId, Guid notificationId, DateTimeOffset dismissedAt)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (notificationId == Guid.Empty)
            throw new DomainException("Notification id must not be empty.");

        return new NotificationDismissal(learnerId, notificationId, dismissedAt);
    }
}
