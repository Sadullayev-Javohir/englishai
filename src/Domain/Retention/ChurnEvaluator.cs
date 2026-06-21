namespace Domain.Retention;

/// <summary>
/// Pure churn-risk scoring (PROJECT-SPEC I.1). Turns a <see cref="ChurnInputs"/> snapshot
/// into the set of fired signals with their risk levels. Free of storage and time
/// concerns so it is fully unit-testable; the Application layer feeds it a snapshot built
/// from the various stores. Uzbek wording is never produced here (docs/development-guide.md rule 11).
/// </summary>
public static class ChurnEvaluator
{
    /// <summary>No activity for this many days is a high-risk signal.</summary>
    public const int NoActivityDays = 7;

    /// <summary>Premium ending within this many days is the renewal-risk window.</summary>
    public const int PremiumExpiryWindowDays = 3;

    /// <summary>
    /// Premium-renewal risk only fires when the learner is also disengaged - at least this
    /// many days without activity - distinguishing it from an active learner who will renew.
    /// </summary>
    public const int LowActivityDays = 2;

    public static ChurnAssessment Evaluate(ChurnInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var signals = new List<ChurnSignal>();

        // Abandoned onboarding is the most damaging - the first impression is lost.
        if (!inputs.OnboardingCompleted)
            signals.Add(new ChurnSignal(ChurnSignalType.OnboardingIncomplete, ChurnRiskLevel.VeryHigh));

        if (inputs.DaysSinceLastActivity >= NoActivityDays)
            signals.Add(new ChurnSignal(ChurnSignalType.NoActivitySevenDays, ChurnRiskLevel.High));

        if (inputs.UnresolvedSpeakingFrustration)
            signals.Add(new ChurnSignal(ChurnSignalType.SpeakingFrustration, ChurnRiskLevel.High));

        if (inputs.StreakLapsed)
            signals.Add(new ChurnSignal(ChurnSignalType.StreakBrokenTwoDays, ChurnRiskLevel.Medium));

        if (inputs.RisingSrsFailures)
            signals.Add(new ChurnSignal(ChurnSignalType.RisingSrsFailures, ChurnRiskLevel.Medium));

        if (inputs.DaysUntilPremiumExpiry is { } days
            && days <= PremiumExpiryWindowDays
            && inputs.DaysSinceLastActivity >= LowActivityDays)
        {
            signals.Add(new ChurnSignal(ChurnSignalType.PremiumLapsingWithLowActivity, ChurnRiskLevel.High));
        }

        return new ChurnAssessment(signals);
    }
}
