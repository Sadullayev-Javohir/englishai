using Application.Assessment.Dtos;
using Application.Assessment.Ports;
using Domain.Assessment;
using MediatR;

namespace Application.Assessment.SubmitSpeaking;

/// <summary>
/// Submits the learner's recorded answer for the placement Speaking stage. The handler
/// scores it with the speaking assessor, records accepted results, and returns either
/// the next item or a typed retryable outcome without advancing the session.
/// </summary>
public sealed record SubmitSpeakingCommand(Guid SessionId, Guid TaskId, byte[] AudioContent)
    : IRequest<SubmitSpeakingResult>;

public sealed record SubmitSpeakingResult(
    int Score,
    CefrLevel Level,
    PlacementSpeakingOutcome Outcome,
    bool Retryable,
    bool IsTestCompleted,
    TestStage CurrentStage,
    CefrLevel CurrentDifficulty,
    PlacementItemDto? NextItem);
