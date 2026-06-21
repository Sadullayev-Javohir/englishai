using Domain.Assessment;
using Domain.Speaking;

namespace Application.Speaking.Dtos;

/// <summary>
/// One roleplay scenario the learner can pick, for the scenario-picker screen (20 per CEFR level,
/// 120 total). The Uzbek title and description are looked up in the frontend content store by
/// <see cref="Code"/> (docs/development-guide.md rule 11); <see cref="EnglishTitle"/> is only a reference/fallback.
/// <see cref="ImageId"/> keys the scenario's licensed illustration in the shared topic-image store
/// (served from /api/images/topics/{imageId} - rule 12).
/// </summary>
public sealed record RoleplayScenarioDto(
    string Code,
    string EnglishTitle,
    CefrLevel Level,
    Guid ImageId)
{
    public static RoleplayScenarioDto FromDomain(RoleplayScenarioDefinition definition) =>
        new(definition.Code, definition.EnglishTitle, definition.Level, definition.ImageId);
}

/// <summary>
/// The end-of-scene "how did you do" result shown to the learner. Carries the numeric scores plus the
/// Uzbek summary/strength/tip strings, which are resolved from vetted templates by the weakest and
/// strongest dimension - the evaluator itself never produces Uzbek text (docs/development-guide.md rule 11).
/// </summary>
/// <param name="Evaluable">
/// False when the sitting had no spoken learner turns to grade (the scores are then all zero and only
/// <see cref="SummaryUz"/> is meaningful - a prompt to actually speak before finishing).
/// </param>
public sealed record RoleplayEvaluationResult(
    bool Evaluable,
    string ScenarioCode,
    double OverallScore,
    double TaskCompletion,
    double Fluency,
    double Grammar,
    double Appropriateness,
    PronunciationBand Band,
    RoleplayDimension StrongestDimension,
    RoleplayDimension WeakestDimension,
    string? SummaryUz,
    string? StrengthUz,
    string? TipUz);
