namespace Domain.Retention;

/// <summary>
/// A single fired churn signal: which behaviour was observed and how risky it is
/// (PROJECT-SPEC I.1).
/// </summary>
public sealed record ChurnSignal(ChurnSignalType Type, ChurnRiskLevel Risk);
