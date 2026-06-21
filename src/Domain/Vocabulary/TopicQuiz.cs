namespace Domain.Vocabulary;

/// <summary>
/// A single cloze ("fill the gap") question built deterministically from a topic's target
/// words (PROJECT-SPEC B.1 - "Matnda tanlash"): the word's example sentence with the word
/// blanked out, plus multiple-choice options drawn from the topic's other words. Questions
/// are derived, not stored: the same words always produce the same questions (seeded by the
/// topic slug), so a submitted answer can be graded server-side without persisting the quiz.
/// </summary>
public sealed record TopicQuizQuestion(
    int Index,
    string Word,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex)
{
    /// <summary>The placeholder that marks the blank in <see cref="Prompt"/>.</summary>
    public const string Blank = "_____";

    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}

/// <summary>The graded outcome of one quiz question (correct answer revealed).</summary>
public sealed record TopicQuizOutcome(
    int QuestionIndex,
    string Word,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect);

/// <summary>The result of grading a vocabulary topic quiz.</summary>
public sealed record TopicQuizResult(
    Guid TopicId,
    int TotalQuestions,
    int CorrectCount,
    IReadOnlyList<TopicQuizOutcome> Outcomes)
{
    public int ScorePercent => TotalQuestions == 0 ? 0 : (int)Math.Round(100.0 * CorrectCount / TotalQuestions);
}
