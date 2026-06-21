using Application.Assessment.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Levels.StartLevelExitTest;

/// <summary>
/// Begins a Level Exit Test (PROJECT-SPEC M.5): a placement-style mini-test that confirms the
/// learner is ready to leave their current CEFR level. It reuses the adaptive placement engine
/// (M.6) but is pinned to start at the learner's current level rather than the neutral default.
/// </summary>
public sealed record StartLevelExitTestCommand(
    Guid LearnerId,
    bool IncludeSpeaking = true,
    CefrLevel? TestLevel = null)
    : IRequest<StartLevelExitTestResult>;

public sealed record StartLevelExitTestResult(
    Guid SessionId,
    CefrLevel Level,
    TestStage CurrentStage,
    LevelExitRequirements Requirements,
    PlacementItemDto? FirstItem);
