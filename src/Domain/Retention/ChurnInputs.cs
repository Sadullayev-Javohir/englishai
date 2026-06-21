namespace Domain.Retention;

/// <summary>
/// A point-in-time snapshot of everything the <see cref="ChurnEvaluator"/> needs to score
/// a learner's drop-off risk (PROJECT-SPEC I.1). Kept as primitives so the evaluator stays
/// pure and fully unit-testable; the Application layer assembles it from the learner
/// profile, gamification store, vocabulary store and subscription.
/// </summary>
public sealed record ChurnInputs
{
    /// <summary>Days since the learner's last recorded activity (0 = active today).</summary>
    public required int DaysSinceLastActivity { get; init; }

    /// <summary>
    /// Whether the learner has completed onboarding (finished the placement test and so
    /// has a learner profile). When false the very-high "abandoned onboarding" signal fires.
    /// </summary>
    public required bool OnboardingCompleted { get; init; }

    /// <summary>
    /// True when the learner had a live streak that has now lapsed (today's goal missed and
    /// the streak is no longer extendable) - the two-consecutive-misses signal.
    /// </summary>
    public bool StreakLapsed { get; init; }

    /// <summary>
    /// True when the most recent speaking activity scored below the frustration threshold
    /// and the learner has not practised anything since (PROJECT-SPEC I.1 frustration row).
    /// </summary>
    public bool UnresolvedSpeakingFrustration { get; init; }

    /// <summary>True when SRS "didn't know" answers are piling up - the level is too hard.</summary>
    public bool RisingSrsFailures { get; init; }

    /// <summary>Days until Premium expires, or <c>null</c> when the learner is not on a paid plan.</summary>
    public int? DaysUntilPremiumExpiry { get; init; }
}
