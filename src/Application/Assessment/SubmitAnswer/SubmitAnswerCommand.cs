using Application.Assessment.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Assessment.SubmitAnswer;

/// <summary>
/// Submits the learner's chosen option for the current question. The handler
/// records the outcome, lets the session adapt the difficulty, and returns the
/// next question (or signals that the test is complete).
/// </summary>
public sealed record SubmitAnswerCommand(
    Guid SessionId,
    Guid QuestionId,
    int SelectedOptionIndex) : IRequest<SubmitAnswerResult>;

public sealed record SubmitAnswerResult(
    bool WasCorrect,
    bool IsTestCompleted,
    TestStage CurrentStage,
    CefrLevel CurrentDifficulty,
    PlacementItemDto? NextItem);
