namespace Domain.Video;

/// <summary>The graded outcome of a single comprehension question.</summary>
public sealed record QuestionOutcome(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? HintCode);

/// <summary>
/// The result of grading a video comprehension quiz. A learner "passes" when at least
/// <see cref="PassThresholdPercent"/> of the questions are correct - the same retrieval
/// gate used to reinforce listening comprehension (PROJECT-SPEC Faza 4).
/// </summary>
public sealed record VideoQuizResult(
    Guid VideoLessonId,
    int TotalQuestions,
    int CorrectCount,
    IReadOnlyList<QuestionOutcome> Outcomes)
{
    /// <summary>Minimum percentage of correct answers required to pass.</summary>
    public const int PassThresholdPercent = 70;

    /// <summary>Percentage of questions answered correctly (0-100).</summary>
    public int ScorePercent => TotalQuestions == 0 ? 0 : (int)Math.Round(100.0 * CorrectCount / TotalQuestions);

    /// <summary>True when the score meets the pass threshold.</summary>
    public bool Passed => TotalQuestions > 0 && ScorePercent >= PassThresholdPercent;
}
