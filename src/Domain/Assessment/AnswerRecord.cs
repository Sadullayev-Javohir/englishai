namespace Domain.Assessment;

/// <summary>
/// Immutable record of a single answered question within a placement session:
/// which stage and difficulty it was, and whether the learner got it right.
/// </summary>
public sealed record AnswerRecord(
    Guid QuestionId,
    TestStage Stage,
    CefrLevel Difficulty,
    bool IsCorrect);
