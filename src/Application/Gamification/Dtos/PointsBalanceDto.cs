namespace Application.Gamification.Dtos;

/// <summary>One coin-for-discount tier, annotated with whether the learner can afford it now.</summary>
public sealed record DiscountTierDto(int CoinsCost, int DiscountPercent, bool CanAfford);

/// <summary>An active (unused, unexpired) discount coupon the learner has already redeemed.</summary>
public sealed record RedemptionDto(string Code, int DiscountPercent, DateTimeOffset ExpiresAt);

/// <summary>The learner's points ledger, the discount catalog, and their active coupons.</summary>
public sealed record PointsBalanceDto(
    long LifetimeXp,
    long SpendableCoins,
    IReadOnlyList<DiscountTierDto> Tiers,
    IReadOnlyList<RedemptionDto> ActiveRedemptions);
