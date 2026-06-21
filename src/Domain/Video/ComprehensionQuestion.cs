using Domain.Common;

namespace Domain.Video;

/// <summary>
/// A multiple-choice comprehension question for a video lesson (Video Lesson Quiz screen).
/// The English prompt and options are curated content; the optional <see cref="HintCode"/>
/// resolves to a vetted Uzbek hint through the content layer (docs/development-guide.md rule 11) rather
/// than being free-generated at runtime.
/// </summary>
public sealed class ComprehensionQuestion
{
    public const int MinOptions = 2;

    // Parameterless ctor for EF Core materialization.
    private ComprehensionQuestion()
    {
        Prompt = null!;
        Options = null!;
    }

    private ComprehensionQuestion(
        string prompt, IReadOnlyList<string> options, int correctOptionIndex, string? hintCode)
    {
        Id = Guid.NewGuid();
        Prompt = prompt;
        Options = options;
        CorrectOptionIndex = correctOptionIndex;
        HintCode = hintCode;
    }

    public Guid Id { get; private set; }

    /// <summary>The English question shown to the learner.</summary>
    public string Prompt { get; private set; }

    /// <summary>The answer choices, in display order.</summary>
    public IReadOnlyList<string> Options { get; private set; }

    /// <summary>Index into <see cref="Options"/> of the correct answer.</summary>
    public int CorrectOptionIndex { get; private set; }

    /// <summary>Optional content-store code for a vetted Uzbek hint (rule 11).</summary>
    public string? HintCode { get; private set; }

    public static ComprehensionQuestion Create(
        string prompt, IReadOnlyList<string> options, int correctOptionIndex, string? hintCode = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Question prompt must not be empty.");
        if (options is null || options.Count < MinOptions)
            throw new DomainException($"A question needs at least {MinOptions} options.");
        if (options.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Question options must not be empty.");
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new DomainException("Correct option index is out of range.");

        return new ComprehensionQuestion(
            prompt.Trim(),
            options.Select(o => o.Trim()).ToList(),
            correctOptionIndex,
            string.IsNullOrWhiteSpace(hintCode) ? null : hintCode.Trim());
    }

    /// <summary>True when <paramref name="selectedOptionIndex"/> is the correct choice.</summary>
    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}
