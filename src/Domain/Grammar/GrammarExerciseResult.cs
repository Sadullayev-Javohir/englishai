namespace Domain.Grammar;

/// <summary>The graded outcome of a single grammar exercise.</summary>
public sealed record GrammarExerciseOutcome(
    Guid ExerciseId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? HintCode,
    string? Explanation = null);

/// <summary>
/// The result of grading a grammar lesson's exercises. A learner "passes" when at least
/// <see cref="PassThresholdPercent"/> of the exercises are correct (PROJECT-SPEC G.2).
/// </summary>
public sealed record GrammarExerciseResult(
    Guid LessonId,
    int TotalExercises,
    int CorrectCount,
    IReadOnlyList<GrammarExerciseOutcome> Outcomes)
{
    /// <summary>Minimum percentage of correct answers required to pass.</summary>
    public const int PassThresholdPercent = 70;

    /// <summary>Percentage of exercises answered correctly (0-100).</summary>
    public int ScorePercent => TotalExercises == 0 ? 0 : (int)Math.Round(100.0 * CorrectCount / TotalExercises);

    /// <summary>True when the score meets the pass threshold.</summary>
    public bool Passed => TotalExercises > 0 && ScorePercent >= PassThresholdPercent;
}
