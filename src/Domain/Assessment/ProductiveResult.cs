namespace Domain.Assessment;

/// <summary>
/// Immutable record of a productive-stage outcome (Writing or Speaking) within a
/// placement session. Unlike the multiple-choice <see cref="AnswerRecord"/>, these
/// stages produce a directly graded 0-100 score from the learner's free text or
/// recorded audio rather than a right/wrong answer.
/// </summary>
public sealed record ProductiveResult(TestStage Stage, int Score, CefrLevel TaskDifficulty);
