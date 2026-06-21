using Domain.Common;

namespace Domain.Subscription;

/// <summary>
/// A learner's subscription (PROJECT-SPEC Qism H). Every learner starts on the free tier;
/// a confirmed payment activates Premium for the plan's duration. Cancelling stops
/// auto-renew but keeps access until the paid period ends; once that passes the daily
/// expiry job transitions the subscription to Expired. All time comparisons take an
/// explicit instant so the logic is testable (docs/development-guide.md 17.2).
/// </summary>
public sealed class Subscription
{
    private Subscription()
    {
    }

    private Subscription(Guid learnerId, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Status = SubscriptionStatus.Free;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public SubscriptionStatus Status { get; private set; }

    /// <summary>The active plan while Premium/Cancelled; null on the free tier.</summary>
    public SubscriptionPlan? Plan { get; private set; }

    /// <summary>When the current paid period ends; null on the free tier.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Subscription CreateFree(Guid learnerId, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        return new Subscription(learnerId, now);
    }

    /// <summary>
    /// Whether the learner currently has Premium access. True while Premium or Cancelled
    /// and the paid period has not yet elapsed.
    /// </summary>
    public bool IsPremiumActive(DateTimeOffset now) =>
        Status is SubscriptionStatus.Premium or SubscriptionStatus.Cancelled &&
        ExpiresAt is { } expiry && expiry > now;

    /// <summary>
    /// Activates (or extends) Premium for the given plan. Extending an already-active
    /// subscription stacks the new duration onto the remaining time; otherwise the period
    /// starts now.
    /// </summary>
    public void Activate(SubscriptionPlan plan, DateTimeOffset now)
    {
        var from = IsPremiumActive(now) ? ExpiresAt!.Value : now;
        Plan = plan;
        ExpiresAt = from.AddDays(SubscriptionPricing.DurationDays(plan));
        Status = SubscriptionStatus.Premium;
        UpdatedAt = now;
    }

    /// <summary>Stops auto-renew. Access is retained until <see cref="ExpiresAt"/>.</summary>
    public void Cancel(DateTimeOffset now)
    {
        if (!IsPremiumActive(now))
            throw new DomainException("Only an active premium subscription can be cancelled.");

        Status = SubscriptionStatus.Cancelled;
        UpdatedAt = now;
    }

    /// <summary>
    /// Transitions a lapsed Premium/Cancelled subscription to Expired. No-op if the period
    /// has not elapsed or the subscription is not in a paid state. Returns whether it changed.
    /// </summary>
    public bool ExpireIfElapsed(DateTimeOffset now)
    {
        var isPaidState = Status is SubscriptionStatus.Premium or SubscriptionStatus.Cancelled;
        if (!isPaidState || ExpiresAt is not { } expiry || expiry > now)
            return false;

        Status = SubscriptionStatus.Expired;
        UpdatedAt = now;
        return true;
    }

    /// <summary>
    /// Whole days remaining until the paid period ends (0 once elapsed); null on the free
    /// tier. Used by the renewal-reminder job (PROJECT-SPEC H.3).
    /// </summary>
    public int? DaysUntilExpiry(DateTimeOffset now)
    {
        if (ExpiresAt is not { } expiry)
            return null;

        var remaining = (expiry - now).TotalDays;
        return remaining <= 0 ? 0 : (int)Math.Ceiling(remaining);
    }
}
