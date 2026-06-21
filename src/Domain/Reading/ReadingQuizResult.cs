namespace Domain.Reading;

/// <summary>The graded outcome of a single reading comprehension question.</summary>
public sealed record ReadingQuestionOutcome(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? HintCode,
    string? Explanation);

/// <summary>
/// The result of grading a reading comprehension quiz. A learner "passes" when at least
/// <see cref="PassThresholdPercent"/> of the questions are correct - the same retrieval
/// gate used across the comprehension modules (PROJECT-SPEC Faza 6).
/// </summary>
public sealed record ReadingQuizResult(
    Guid PassageId,
    int TotalQuestions,
    int CorrectCount,
    IReadOnlyList<ReadingQuestionOutcome> Outcomes)
{
    /// <summary>Minimum percentage of correct answers required to pass.</summary>
    public const int PassThresholdPercent = 75;

    /// <summary>Percentage of questions answered correctly (0-100).</summary>
    public int ScorePercent => TotalQuestions == 0 ? 0 : (int)Math.Round(100.0 * CorrectCount / TotalQuestions);

    /// <summary>True when the score meets the pass threshold.</summary>
    public bool Passed => TotalQuestions > 0 && ScorePercent >= PassThresholdPercent;
}
