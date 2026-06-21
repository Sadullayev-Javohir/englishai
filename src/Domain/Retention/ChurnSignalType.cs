namespace Domain.Retention;

/// <summary>
/// The early-warning behaviours tracked as churn signals (PROJECT-SPEC I.1 table). Each
/// maps to a structured code only - any Uzbek wording shown to the learner is resolved
/// from vetted templates elsewhere (docs/development-guide.md rule 11).
/// </summary>
public enum ChurnSignalType
{
    /// <summary>Daily goal missed two days in a row - streak broken (Medium).</summary>
    StreakBrokenTwoDays,

    /// <summary>No activity at all for seven days (High).</summary>
    NoActivitySevenDays,

    /// <summary>A low speaking score with no follow-up attempt - frustration (High).</summary>
    SpeakingFrustration,

    /// <summary>Rising streak of "didn't know" SRS answers - level too hard now (Medium).</summary>
    RisingSrsFailures,

    /// <summary>Onboarding abandoned - placement test left unfinished (Very high).</summary>
    OnboardingIncomplete,

    /// <summary>Premium ends within days while activity is low - unlikely to renew (High).</summary>
    PremiumLapsingWithLowActivity
}
