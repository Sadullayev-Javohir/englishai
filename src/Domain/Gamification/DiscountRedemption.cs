using Domain.Common;

namespace Domain.Gamification;

/// <summary>Lifecycle of a redeemed discount coupon.</summary>
public enum DiscountRedemptionStatus
{
    Active = 0,
    Used = 1,
    Expired = 2,
}

/// <summary>
/// A one-time Pro-discount coupon a learner bought with <see cref="LearnerPoints.SpendableCoins"/>
/// (PROJECT-SPEC-adjacent leaderboard/points feature). Redeemed coupons expire after
/// <see cref="ExpiryDays"/> days if never applied at checkout.
/// </summary>
public sealed class DiscountRedemption
{
    public const int ExpiryDays = 30;

    private DiscountRedemption()
    {
        Code = null!;
    }

    private DiscountRedemption(Guid learnerId, DiscountTier tier, string code, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        CoinsSpent = tier.CoinsCost;
        DiscountPercent = tier.DiscountPercent;
        Code = code;
        Status = DiscountRedemptionStatus.Active;
        CreatedAt = now;
        ExpiresAt = now.AddDays(ExpiryDays);
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public int CoinsSpent { get; private set; }
    public int DiscountPercent { get; private set; }
    public string Code { get; private set; }
    public DiscountRedemptionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public static DiscountRedemption Create(Guid learnerId, DiscountTier tier, string code, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Redemption code must not be empty.");
        if (tier.DiscountPercent <= 0 || tier.DiscountPercent > DiscountCatalog.MaximumDiscountPercent)
            throw new DomainException("Discount percent exceeds the promotional safety limit.");

        return new DiscountRedemption(learnerId, tier, code, now);
    }

    /// <summary>Whether this coupon can still be applied at checkout.</summary>
    public bool IsUsable(DateTimeOffset now) => Status == DiscountRedemptionStatus.Active && now <= ExpiresAt;

    /// <summary>Consumes the coupon at checkout. Throws if it is not currently usable.</summary>
    public void MarkUsed(DateTimeOffset now)
    {
        if (!IsUsable(now))
            throw new DomainException("This discount code is no longer usable.");

        Status = DiscountRedemptionStatus.Used;
        UsedAt = now;
    }

    /// <summary>Transitions a lapsed, never-used coupon to Expired. No-op otherwise.</summary>
    public void ExpireIfElapsed(DateTimeOffset now)
    {
        if (Status == DiscountRedemptionStatus.Active && now > ExpiresAt)
            Status = DiscountRedemptionStatus.Expired;
    }
}
