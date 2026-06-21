namespace Domain.Retention;

/// <summary>
/// Severity of a churn (drop-off) risk signal (PROJECT-SPEC I.1). Ordered so the overall
/// risk of an assessment is simply the maximum of its signals. <see cref="None"/> means
/// the signal did not fire.
/// </summary>
public enum ChurnRiskLevel
{
    None = 0,

    /// <summary>"O'rta" - a soft warning worth a gentle nudge.</summary>
    Medium = 1,

    /// <summary>"Yuqori" - strong drop-off risk; act before the learner disengages.</summary>
    High = 2,

    /// <summary>"Juda yuqori" - first impression lost (e.g. onboarding abandoned).</summary>
    VeryHigh = 3
}
