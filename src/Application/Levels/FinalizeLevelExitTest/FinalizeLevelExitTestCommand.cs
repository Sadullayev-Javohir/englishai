using Application.Assessment.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Levels.FinalizeLevelExitTest;

/// <summary>
/// Finalizes a Level Exit Test (PROJECT-SPEC M.5). The completed placement-style session is
/// scored; the outcome is recorded as the G.4 confirmation mini-test and, if the learner also
/// meets the skill-mastery bar, advances them to the next CEFR level.
/// </summary>
public sealed record FinalizeLevelExitTestCommand(Guid SessionId)
    : IRequest<FinalizeLevelExitTestResult>;

/// <summary>
/// Outcome of an exit test. <paramref name="Passed"/> is whether the mini-test itself was passed
/// (the confirmation gate); <paramref name="Advanced"/> is whether that, combined with the
/// skill-mastery bar (G.4), moved the learner up a level. When passed but not advanced,
/// <paramref name="MasteredSkillCount"/>/<paramref name="RequiredMasteredSkills"/> explain why.
/// </summary>
public sealed record FinalizeLevelExitTestResult(
    bool Passed,
    bool Advanced,
    CefrLevel TestedLevel,
    CefrLevel NewLevel,
    int MasteredSkillCount,
    int RequiredMasteredSkills,
    double MinimumOverallScore,
    double MinimumStageScore,
    double ProductiveStageFloor,
    IReadOnlyList<TestStage> FailedStages,
    PlacementResultDto Result);
