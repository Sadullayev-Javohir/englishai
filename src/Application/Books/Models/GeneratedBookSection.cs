namespace Application.Books.Models;

/// <summary>
/// One generated comprehension question for a book section: an English prompt, answer options,
/// the index of the correct option and an English explanation of why it is correct.
/// </summary>
public sealed record GeneratedBookQuestion(
    string Prompt, IReadOnlyList<string> Options, int CorrectOptionIndex, string? Explanation);

/// <summary>
/// The LLM-generated content for one book section: the English section text plus its
/// comprehension questions. Produced by an <see cref="Ports.IBookContentGenerator"/> and cached
/// on the section (docs/development-guide.md rules 8, 10). A book section requires exactly
/// <c>BookSection.QuestionsPerSection</c> (10) valid questions to be filled.
/// </summary>
public sealed record GeneratedBookSection(
    string Body,
    IReadOnlyList<GeneratedBookQuestion> Questions)
{
    /// <summary>An empty result, signalling generation was unavailable (the section stays pending).</summary>
    public static GeneratedBookSection Empty { get; } =
        new(string.Empty, Array.Empty<GeneratedBookQuestion>());

    /// <summary>True when there is a usable body with at least one comprehension question.</summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Body) && Questions.Count > 0;
}
