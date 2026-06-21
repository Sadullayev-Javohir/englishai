using Domain.Common;

namespace Domain.Notifications;

/// <summary>
/// A learner's registered push-notification target: one FCM registration token per device/install.
/// Recorded so the server can deliver native push (status-bar notification + app badge) even when
/// the app is closed. A token is unique (the same install re-registering just refreshes
/// <see cref="LastSeenAt"/> and re-points it at the current owner); tokens FCM reports as invalid are
/// pruned, so the table stays a live set of reachable devices.
/// </summary>
public sealed class DeviceToken
{
    // Parameterless ctor for EF Core materialization.
    private DeviceToken()
    {
        Token = null!;
        Platform = null!;
    }

    private DeviceToken(Guid id, Guid userAccountId, string token, string platform, DateTimeOffset now)
    {
        Id = id;
        UserAccountId = userAccountId;
        Token = token;
        Platform = platform;
        CreatedAt = now;
        LastSeenAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>The learner who owns this device (the account the app is signed in as).</summary>
    public Guid UserAccountId { get; private set; }

    /// <summary>The FCM registration token - the address a push is sent to.</summary>
    public string Token { get; private set; }

    /// <summary>Originating platform: <c>android</c>, <c>ios</c> or <c>web</c>.</summary>
    public string Platform { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Last time this token was re-registered - used to age out stale devices.</summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    public static DeviceToken Create(Guid userAccountId, string token, string platform, DateTimeOffset now)
    {
        if (userAccountId == Guid.Empty)
            throw new DomainException("Device token must belong to a learner.");
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Device token must not be empty.");
        if (string.IsNullOrWhiteSpace(platform))
            throw new DomainException("Device platform must not be empty.");

        return new DeviceToken(Guid.NewGuid(), userAccountId, token.Trim(), platform.Trim().ToLowerInvariant(), now);
    }

    /// <summary>Re-points an existing token at its current owner and refreshes its last-seen time.</summary>
    public void Refresh(Guid userAccountId, string platform, DateTimeOffset now)
    {
        UserAccountId = userAccountId;
        Platform = platform.Trim().ToLowerInvariant();
        LastSeenAt = now;
    }
}
