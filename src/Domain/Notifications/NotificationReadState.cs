using Domain.Common;

namespace Domain.Notifications;

/// <summary>
/// A learner's "notifications read" watermark. Most in-app notifications are <em>derived</em>
/// (SRS review reminders and the daily activity reminders are recomputed on every fetch from the
/// learner's current state, not stored one row each), so a per-notification read flag has nowhere
/// durable to live. Instead we persist a single timestamp per learner: a notification counts as
/// read once its <c>CreatedAt</c> is at or before <see cref="LastReadAt"/>.
///
/// This is what makes "Hammasini o'qish" actually dismiss reminders - tapping it moves the
/// watermark to now, so everything currently shown becomes read and stays read across restarts,
/// while genuinely new notifications (a word that becomes due tomorrow, the next day's plan) have a
/// later <c>CreatedAt</c> and surface again.
/// </summary>
public sealed class NotificationReadState
{
    // Parameterless ctor for EF Core materialization.
    private NotificationReadState()
    {
    }

    private NotificationReadState(Guid learnerId, DateTimeOffset lastReadAt)
    {
        LearnerId = learnerId;
        LastReadAt = lastReadAt;
    }

    /// <summary>The learner this watermark belongs to (primary key).</summary>
    public Guid LearnerId { get; private set; }

    /// <summary>The moment the learner last marked their notifications read.</summary>
    public DateTimeOffset LastReadAt { get; private set; }

    public static NotificationReadState Create(Guid learnerId, DateTimeOffset lastReadAt)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        return new NotificationReadState(learnerId, lastReadAt);
    }

    /// <summary>Advances the watermark; never moves it backwards.</summary>
    public void MarkReadAt(DateTimeOffset moment)
    {
        if (moment > LastReadAt)
            LastReadAt = moment;
    }
}
