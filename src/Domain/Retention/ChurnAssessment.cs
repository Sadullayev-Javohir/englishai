namespace Domain.Retention;

/// <summary>
/// The result of scoring a learner's drop-off risk (PROJECT-SPEC I.1): the list of fired
/// signals and the overall risk, which is the highest individual signal. Pure data.
/// </summary>
public sealed class ChurnAssessment
{
    public ChurnAssessment(IReadOnlyList<ChurnSignal> signals)
    {
        Signals = signals;
        OverallRisk = signals.Count == 0
            ? ChurnRiskLevel.None
            : signals.Max(s => s.Risk);
    }

    /// <summary>Every signal that fired, in the order the evaluator produced them.</summary>
    public IReadOnlyList<ChurnSignal> Signals { get; }

    /// <summary>The maximum risk across all fired signals (<see cref="ChurnRiskLevel.None"/> if none).</summary>
    public ChurnRiskLevel OverallRisk { get; }

    /// <summary>Whether the learner shows any churn risk at all.</summary>
    public bool IsAtRisk => OverallRisk != ChurnRiskLevel.None;
}
