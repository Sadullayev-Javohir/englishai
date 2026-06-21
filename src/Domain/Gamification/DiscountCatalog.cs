namespace Domain.Gamification;

/// <summary>A fixed coin cost that unlocks a percent-off Pro coupon.</summary>
public sealed record DiscountTier(int CoinsCost, int DiscountPercent);

/// <summary>
/// The fixed set of coin-for-discount tiers a learner can redeem
/// (<see cref="LearnerPoints.SpendableCoins"/> against a Pro subscription discount).
/// </summary>
public static class DiscountCatalog
{
    /// <summary>
    /// Marketing safety ceiling. A coupon may never remove more than 30% of the plan price,
    /// preserving at least 70% gross revenue before payment-provider and operating costs.
    /// </summary>
    public const int MaximumDiscountPercent = 30;

    public const int MinimumPayablePercent = 100 - MaximumDiscountPercent;

    public static readonly IReadOnlyList<DiscountTier> Tiers = new[]
    {
        new DiscountTier(CoinsCost: 4_000, DiscountPercent: 5),
        new DiscountTier(CoinsCost: 8_000, DiscountPercent: 10),
        new DiscountTier(CoinsCost: 16_000, DiscountPercent: 20),
        new DiscountTier(CoinsCost: 24_000, DiscountPercent: 30),
    };

    static DiscountCatalog()
    {
        if (Tiers.Any(tier => tier.CoinsCost <= 0
            || tier.DiscountPercent <= 0
            || tier.DiscountPercent > MaximumDiscountPercent))
        {
            throw new InvalidOperationException("Discount catalog violates the promotional safety limits.");
        }
    }

    /// <summary>The tier matching this exact coin cost, or null if it is not a valid tier.</summary>
    public static DiscountTier? FindByCoinsCost(int coinsCost) =>
        Tiers.FirstOrDefault(t => t.CoinsCost == coinsCost);

    /// <summary>Applies one XP coupon while enforcing the global revenue floor.</summary>
    public static int CalculateDiscountedAmount(int originalAmountUzs, int discountPercent)
    {
        if (originalAmountUzs <= 0)
            throw new ArgumentOutOfRangeException(nameof(originalAmountUzs));
        if (discountPercent < 0 || discountPercent > MaximumDiscountPercent)
            throw new ArgumentOutOfRangeException(nameof(discountPercent));

        var discounted = originalAmountUzs - originalAmountUzs * discountPercent / 100;
        var minimumPayable = (int)Math.Ceiling(originalAmountUzs * MinimumPayablePercent / 100m);
        return Math.Max(discounted, minimumPayable);
    }
}
