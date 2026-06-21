using Domain.Retention;

namespace Application.Retention.Dtos;

/// <summary>
/// A learner's drop-off risk for retention dashboards (PROJECT-SPEC I.1): the overall
/// risk and the individual signals that fired, as structured codes (no Uzbek wording).
/// </summary>
public sealed record ChurnAssessmentDto(
    Guid LearnerId,
    ChurnRiskLevel OverallRisk,
    bool IsAtRisk,
    IReadOnlyList<ChurnSignalDto> Signals)
{
    public static ChurnAssessmentDto From(Guid learnerId, ChurnAssessment assessment) =>
        new(
            learnerId,
            assessment.OverallRisk,
            assessment.IsAtRisk,
            assessment.Signals.Select(s => new ChurnSignalDto(s.Type, s.Risk)).ToList());
}

public sealed record ChurnSignalDto(ChurnSignalType Type, ChurnRiskLevel Risk);
