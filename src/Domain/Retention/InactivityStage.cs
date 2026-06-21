namespace Domain.Retention;

/// <summary>
/// The staged win-back ladder based on how many days a learner has been away
/// (PROJECT-SPEC I.2). This is unrelated to the SRS 3/7/21 review schedule - it targets
/// learners who have stopped opening the app at all.
/// </summary>
public enum InactivityStage
{
    /// <summary>Active recently - no win-back message.</summary>
    Active = 0,

    /// <summary>3 days away - a gentle, encouraging nudge.</summary>
    Day3 = 3,

    /// <summary>7 days away - progress reminder + an easier "come back" offer.</summary>
    Day7 = 7,

    /// <summary>14 days away - personalised reminder around the learner's strongest module.</summary>
    Day14 = 14,

    /// <summary>30 days away - a comeback bonus + short "why did you stop" survey.</summary>
    Day30 = 30,

    /// <summary>60+ days away - low-frequency, non-intrusive reminder (monthly).</summary>
    Day60Plus = 60
}
