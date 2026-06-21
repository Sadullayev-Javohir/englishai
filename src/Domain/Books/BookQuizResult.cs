namespace Domain.Books;

/// <summary>The graded outcome of a single book comprehension question.</summary>
public sealed record BookQuestionOutcome(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Explanation);

/// <summary>
/// The result of grading a book section's comprehension quiz. A learner "reads" the section when
/// at least <see cref="PassThresholdPercent"/> of the questions are answered correctly. Below that
/// the section is not credited and the learner is asked to try again.
/// </summary>
public sealed record BookQuizResult(
    Guid SectionId,
    int TotalQuestions,
    int CorrectCount,
    IReadOnlyList<BookQuestionOutcome> Outcomes)
{
    public const int PassThresholdPercent = 70;

    public int RequiredCorrect => (int)Math.Ceiling(TotalQuestions * PassThresholdPercent / 100.0);

    /// <summary>Percentage of questions answered correctly (0-100).</summary>
    public int ScorePercent => TotalQuestions == 0 ? 0 : (int)Math.Round(100.0 * CorrectCount / TotalQuestions);

    public bool Passed =>
        TotalQuestions > 0 && CorrectCount * 100 >= TotalQuestions * PassThresholdPercent;
}
