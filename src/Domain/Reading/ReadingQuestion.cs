using Domain.Common;

namespace Domain.Reading;

/// <summary>
/// A multiple-choice comprehension question for a reading passage. The English prompt and
/// options are curated/generated content; the optional <see cref="HintCode"/> resolves to a vetted
/// Uzbek hint through the content layer (docs/development-guide.md rule 11), while <see cref="Explanation"/> is an
/// English "why this answer is correct" note - English is target-language teaching content the
/// model may author, which is what the topic-scoped generator produces (the immersion path).
/// </summary>
public sealed class ReadingQuestion
{
    public const int MinOptions = 2;

    // Parameterless ctor for EF Core materialization.
    private ReadingQuestion()
    {
        Prompt = null!;
        Options = null!;
    }

    private ReadingQuestion(
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

    /// <summary>Optional content-store code for a vetted Uzbek hint (rule 11).</summary>
    public string? HintCode { get; private set; }

    /// <summary>Optional English explanation of the correct answer (generated teaching content).</summary>
    public string? Explanation { get; private set; }

    public static ReadingQuestion Create(
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

        return new ReadingQuestion(
            prompt.Trim(),
            options.Select(o => o.Trim()).ToList(),
            correctOptionIndex,
            string.IsNullOrWhiteSpace(hintCode) ? null : hintCode.Trim(),
            string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim());
    }

    /// <summary>True when <paramref name="selectedOptionIndex"/> is the correct choice.</summary>
    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}
