using Domain.Common;

namespace Domain.Assessment;

/// <summary>
/// A single multiple-choice item in the placement question bank, tagged with the
/// stage it belongs to and its CEFR difficulty so the adaptive engine can request
/// items of an appropriate level. The question prompt/options text itself is
/// content (English) and is stored as data, not generated at runtime.
/// </summary>
public sealed class PlacementQuestion
{
    private readonly List<string> _options = new();

    private PlacementQuestion(
        Guid id,
        TestStage stage,
        CefrLevel difficulty,
        string prompt,
        IEnumerable<string> options,
        int correctOptionIndex,
        string? audioScript,
        string? passageText)
    {
        Id = id;
        Stage = stage;
        Difficulty = difficulty;
        Prompt = prompt;
        _options.AddRange(options);
        CorrectOptionIndex = correctOptionIndex;
        AudioScript = audioScript;
        PassageText = passageText;
    }

    public Guid Id { get; }
    public TestStage Stage { get; }
    public CefrLevel Difficulty { get; }
    public string Prompt { get; }
    public IReadOnlyList<string> Options => _options;
    public int CorrectOptionIndex { get; }

    /// <summary>
    /// For listening items, the text spoken aloud by TTS. It is deliberately kept
    /// out of <see cref="Prompt"/> and never sent to the browser as text, so the
    /// learner must understand it by ear rather than read the answer. Null for
    /// non-listening items.
    /// </summary>
    public string? AudioScript { get; }

    /// <summary>True when this item is delivered as audio (a listening item).</summary>
    public bool HasAudio => !string.IsNullOrWhiteSpace(AudioScript);

    /// <summary>
    /// For reading items, the passage the learner reads before answering the
    /// comprehension question. Unlike the listening <see cref="AudioScript"/>, this
    /// text is shown to the learner. Null for non-reading items.
    /// </summary>
    public string? PassageText { get; }

    public static PlacementQuestion Create(
        Guid id,
        TestStage stage,
        CefrLevel difficulty,
        string prompt,
        IReadOnlyList<string> options,
        int correctOptionIndex,
        string? audioScript = null,
        string? passageText = null)
    {
        if (id == Guid.Empty)
            throw new DomainException("Question id must not be empty.");
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Question prompt must not be empty.");
        if (options is null || options.Count < 2)
            throw new DomainException("A multiple-choice question requires at least two options.");
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new DomainException("Correct option index is out of range.");

        return new PlacementQuestion(
            id, stage, difficulty, prompt, options, correctOptionIndex, audioScript, passageText);
    }

    /// <summary>True when the supplied option index matches the correct answer.</summary>
    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}
