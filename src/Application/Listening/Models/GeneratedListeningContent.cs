namespace Application.Listening.Models;

/// <summary>
/// One generated comprehension question: an English prompt, answer options, the index of the
/// correct option and an English explanation of why it is correct (the immersion teaching note).
/// </summary>
public sealed record GeneratedListeningQuestion(
    string Prompt, IReadOnlyList<string> Options, int CorrectOptionIndex, string? Explanation);

/// <summary>
/// The LLM-generated listening exercise for a learning-spine topic: a short, CEFR-leveled English
/// transcript (the spoken script, synthesized to audio by Azure TTS and cached - rule 10) and a set
/// of comprehension questions with English explanations. Produced by an
/// <see cref="Ports.IListeningContentGenerator"/> and cached on the exercise (rules 8, 10).
/// </summary>
public sealed record GeneratedListeningContent(
    string Transcript,
    IReadOnlyList<GeneratedListeningQuestion> Questions)
{
    /// <summary>An empty result, signalling generation was unavailable (exercise stays pending).</summary>
    public static GeneratedListeningContent Empty { get; } =
        new(string.Empty, Array.Empty<GeneratedListeningQuestion>());

    /// <summary>True when there is a usable transcript with at least one comprehension question.</summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Transcript) && Questions.Count > 0;
}
