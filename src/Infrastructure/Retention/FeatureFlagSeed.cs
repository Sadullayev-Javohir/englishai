using Domain.Retention;

namespace Infrastructure.Retention;

/// <summary>
/// The starter feature-flag experiments for the growth stage (PROJECT-SPEC I.3). The
/// content/growth team extends this set. The canonical example from the spec is comparing
/// daily-goal sizes (3 vs 5 tasks) for retention; the variant <c>Value</c> carries the
/// structured payload (the goal size) the feature reads. No Uzbek wording lives here.
/// </summary>
public static class FeatureFlagSeed
{
    /// <summary>Flag key for the daily-goal-size experiment.</summary>
    public const string DailyGoalSizeKey = "daily_goal_size";

    public static IReadOnlyList<FeatureFlag> Flags() => new List<FeatureFlag>
    {
        new(
            DailyGoalSizeKey,
            "Daily goal size A/B: 3 tasks (control) vs 5 tasks (PROJECT-SPEC I.3).",
            enabled: true,
            new[]
            {
                new FeatureVariant("control", weight: 1, value: "3"),
                new FeatureVariant("variant_five", weight: 1, value: "5")
            })
    };
}
