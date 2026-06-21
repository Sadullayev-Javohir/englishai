using Domain.Common;

namespace Domain.Listening;

/// <summary>
/// A multiple-choice comprehension question for a listening exercise. The English prompt and
/// options are curated content. A question carries either a content-store <see cref="HintCode"/>
/// (the seeded path - resolves to a vetted Uzbek hint, rule 11) or an English
/// <see cref="Explanation"/> (the generated path - an immersion teaching note, the sanctioned
/// dynamic path of rule 11, the same as a reading/grammar question). Both are shown only when the
/// learner's answer was wrong.
/// </summary>
public sealed class ListeningQuestion
{
    public const int MinOptions = 2;

    // Parameterless ctor for EF Core materialization.
    private ListeningQuestion()
    {
        Prompt = null!;
        Options = null!;
    }

    private ListeningQuestion(
        string prompt, IReadOnlyList<string> options, int correctOptionIndex, string? hintCode, string? explanation)
    {
        Id = Guid.NewGuid();
        Prompt = prompt;
        Options = options;
        CorrectOptionIndex = correctOptionIndex;
        HintCode = hintCode;
        Explanation = explanation;
    }

    public Guid Id { get; private set; }

    /// <summary>The English question shown to the learner.</summary>
    public string Prompt { get; private set; }

    /// <summary>The answer choices, in display order.</summary>
    public IReadOnlyList<string> Options { get; private set; }

    /// <summary>Index into <see cref="Options"/> of the correct answer.</summary>
    public int CorrectOptionIndex { get; private set; }

    /// <summary>Optional content-store code for a vetted Uzbek hint (seeded path, rule 11).</summary>
    public string? HintCode { get; private set; }

    /// <summary>
    /// Optional English "why this is correct" note (generated path) - immersion teaching content
    /// shown only for a wrong answer, the same as a reading/grammar question's explanation.
    /// </summary>
    public string? Explanation { get; private set; }

    public static ListeningQuestion Create(
        string prompt, IReadOnlyList<string> options, int correctOptionIndex,
        string? hintCode = null, string? explanation = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Question prompt must not be empty.");
        if (options is null || options.Count < MinOptions)
            throw new DomainException($"A question needs at least {MinOptions} options.");
        if (options.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Question options must not be empty.");
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new DomainException("Correct option index is out of range.");

        return new ListeningQuestion(
            prompt.Trim(),
            options.Select(o => o.Trim()).ToList(),
            correctOptionIndex,
            string.IsNullOrWhiteSpace(hintCode) ? null : hintCode.Trim(),
            string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim());
    }

    public void AdminUpdate(
        string prompt,
        IReadOnlyList<string> options,
        int correctOptionIndex,
        string? hintCode,
        string? explanation)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Question prompt must not be empty.");
        if (options is null || options.Count < MinOptions || options.Any(string.IsNullOrWhiteSpace))
            throw new DomainException($"A listening question needs at least {MinOptions} non-empty options.");
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new DomainException("Correct option index is out of range.");

        Prompt = prompt.Trim();
        Options = options.Select(option => option.Trim()).ToList();
        CorrectOptionIndex = correctOptionIndex;
        HintCode = string.IsNullOrWhiteSpace(hintCode) ? null : hintCode.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
    }

    /// <summary>True when <paramref name="selectedOptionIndex"/> is the correct choice.</summary>
    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}
