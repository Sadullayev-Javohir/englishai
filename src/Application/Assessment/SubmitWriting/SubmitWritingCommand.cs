using Application.Assessment.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Assessment.SubmitWriting;

/// <summary>
/// Submits the learner's free-text answer for the placement Writing stage. The handler
/// scores it with the writing assessor, records the 0-100 productive result, and returns
/// the next item (or signals that the test is complete).
/// </summary>
public sealed record SubmitWritingCommand(Guid SessionId, Guid TaskId, string Text)
    : IRequest<SubmitWritingResult>;

public sealed record SubmitWritingResult(
    int Score,
    CefrLevel Level,
    bool IsTestCompleted,
    TestStage CurrentStage,
    CefrLevel CurrentDifficulty,
    PlacementItemDto? NextItem);
