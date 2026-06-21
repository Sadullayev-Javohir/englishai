namespace Domain.Subscription;

/// <summary>
/// Plan durations and prices (PROJECT-SPEC H.1). Prices are in Uzbek so'm, tuned to local
/// purchasing power - a language centre charges several times this per month.
///
/// The monthly rate has to clear the variable cost of an engaged learner (speech-to-text, synthesis
/// and the assessment calls), not just look affordable: below roughly 90 000 so'm a learner who
/// actually uses their daily speaking budget costs more than they pay, so growth would make the
/// product lose money faster. The longer the commitment, the lower the effective monthly rate:
/// 3 months ~16% off, 6 months ~24% off, 1 year ~33% off vs the monthly rate.
///
/// Centralized here so billing and gateway code never hard-code amounts, and served to the frontend
/// through the plan catalog endpoint rather than copied into the UI.
/// </summary>
public static class SubscriptionPricing
{
    public const int MonthlyDurationDays = 30;
    public const int QuarterlyDurationDays = 90;
    public const int SemiAnnualDurationDays = 180;
    public const int YearlyDurationDays = 365;

    public const int MonthlyPriceUzs = 99_000;
    public const int QuarterlyPriceUzs = 249_000;
    public const int SemiAnnualPriceUzs = 449_000;
    public const int YearlyPriceUzs = 799_000;

    /// <summary>
    /// The effective monthly rate for a plan, used to show the saving against
    /// <see cref="MonthlyPriceUzs"/>. Derived rather than written down, because the two numbers
    /// drifting apart is exactly how a pricing page starts lying.
    /// </summary>
    public static int PricePerMonthUzs(SubscriptionPlan plan) =>
        (int)Math.Round(PriceUzs(plan) * (double)MonthlyDurationDays / DurationDays(plan));

    /// <summary>How much cheaper per month this plan is than paying monthly, as a whole percent.</summary>
    public static int SavePercent(SubscriptionPlan plan) =>
        (int)Math.Round((1 - PricePerMonthUzs(plan) / (double)MonthlyPriceUzs) * 100);

    public static int DurationDays(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Monthly => MonthlyDurationDays,
        SubscriptionPlan.Quarterly => QuarterlyDurationDays,
        SubscriptionPlan.SemiAnnual => SemiAnnualDurationDays,
        SubscriptionPlan.Yearly => YearlyDurationDays,
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "Unknown subscription plan."),
    };

    public static int PriceUzs(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Monthly => MonthlyPriceUzs,
        SubscriptionPlan.Quarterly => QuarterlyPriceUzs,
        SubscriptionPlan.SemiAnnual => SemiAnnualPriceUzs,
        SubscriptionPlan.Yearly => YearlyPriceUzs,
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "Unknown subscription plan."),
    };
}
