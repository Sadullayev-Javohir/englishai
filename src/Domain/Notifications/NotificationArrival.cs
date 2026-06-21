using Domain.Common;

namespace Domain.Notifications;

/// <summary>
/// Records the first moment a <em>derived</em> daily reminder was observed for a learner on a given
/// day. The daily activity reminders are recomputed on every fetch (so practiced skills drop off
/// live), which leaves them with no natural "sent at" time - previously they were stamped with local
/// midnight, which read as a misleading <c>00:00</c>. Persisting a stable first-seen timestamp per
/// <see cref="Key"/> gives each reminder a genuine arrival time that survives restarts while keeping
/// the read-watermark stable (the timestamp never moves once recorded).
/// </summary>
public sealed class NotificationArrival
{
    // Parameterless ctor for EF Core materialization.
    private NotificationArrival()
    {
        Key = null!;
    }

    private NotificationArrival(string key, DateTimeOffset firstSeenAt)
    {
        Key = key;
        FirstSeenAt = firstSeenAt;
    }

    /// <summary>Stable identity of the reminder occurrence: <c>{learnerId:N}|{code}|{yyyy-MM-dd}</c>.</summary>
    public string Key { get; private set; }

    /// <summary>The first time this reminder was computed for its learner - its real arrival time.</summary>
    public DateTimeOffset FirstSeenAt { get; private set; }

    public static NotificationArrival Create(string key, DateTimeOffset firstSeenAt)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Notification arrival key must not be empty.");

        return new NotificationArrival(key, firstSeenAt);
    }
}
