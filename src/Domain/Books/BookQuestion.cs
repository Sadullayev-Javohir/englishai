using Domain.Common;

namespace Domain.Books;

/// <summary>
/// A multiple-choice comprehension question for a book section. The English prompt and options are
/// generated teaching content; <see cref="Explanation"/> is an English "why this answer is correct"
/// note shown after grading (the immersion path - English is target-language teaching content the
/// model may author, mirroring <c>ReadingQuestion</c>, docs/development-guide.md rule 11).
/// </summary>
public sealed class BookQuestion
{
    public const int MinOptions = 2;

    // Parameterless ctor for EF Core materialization.
    private BookQuestion()
    {
        Prompt = null!;
        Options = null!;
    }

    private BookQuestion(string prompt, IReadOnlyList<string> options, int correctOptionIndex, string? explanation)
    {
        Id = Guid.NewGuid();
        Prompt = prompt;
        Options = options;
        CorrectOptionIndex = correctOptionIndex;
        Explanation = explanation;
    }

    public Guid Id { get; private set; }

    /// <summary>The English question shown to the learner.</summary>
    public string Prompt { get; private set; }

    /// <summary>The answer choices, in display order.</summary>
    public IReadOnlyList<string> Options { get; private set; }

    /// <summary>Index into <see cref="Options"/> of the correct answer.</summary>
    public int CorrectOptionIndex { get; private set; }

    /// <summary>Optional English explanation of the correct answer (generated teaching content).</summary>
    public string? Explanation { get; private set; }

    public static BookQuestion Create(
        string prompt, IReadOnlyList<string> options, int correctOptionIndex, string? explanation = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Question prompt must not be empty.");
        if (options is null || options.Count < MinOptions)
            throw new DomainException($"A question needs at least {MinOptions} options.");
        if (options.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Question options must not be empty.");
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new DomainException("Correct option index is out of range.");

        return new BookQuestion(
            prompt.Trim(),
            options.Select(o => o.Trim()).ToList(),
            correctOptionIndex,
            string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim());
    }

    /// <summary>True when <paramref name="selectedOptionIndex"/> is the correct choice.</summary>
    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}
