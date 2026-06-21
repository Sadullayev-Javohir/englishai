namespace Application.Reading.Models;

/// <summary>One interactive glossary word: the English word, a short Uzbek meaning and an example.</summary>
public sealed record GeneratedReadingGlossary(string Word, string Translation, string? ExampleSentence);

/// <summary>
/// One generated comprehension question: an English prompt, answer options, the index of the
/// correct option and an English explanation of why it is correct (the immersion teaching note).
/// </summary>
public sealed record GeneratedReadingQuestion(
    string Prompt, IReadOnlyList<string> Options, int CorrectOptionIndex, string? Explanation);

/// <summary>
/// The LLM-generated reading lesson for a learning-spine topic: a CEFR-leveled English passage,
/// an interactive glossary (the Uzbek support words - richer at A1–A2, sparser by B1+) and a set
/// of comprehension questions with English explanations. Produced by an
/// <see cref="Ports.IReadingContentGenerator"/> and cached on the passage (rules 8, 10).
/// </summary>
public sealed record GeneratedReadingContent(
    string Body,
    IReadOnlyList<GeneratedReadingGlossary> Glossary,
    IReadOnlyList<GeneratedReadingQuestion> Questions)
{
    /// <summary>An empty result, signalling generation was unavailable (lesson stays pending).</summary>
    public static GeneratedReadingContent Empty { get; } =
        new(string.Empty, Array.Empty<GeneratedReadingGlossary>(), Array.Empty<GeneratedReadingQuestion>());

    /// <summary>True when there is a usable passage body with at least one comprehension question.</summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Body) && Questions.Count > 0;
}
