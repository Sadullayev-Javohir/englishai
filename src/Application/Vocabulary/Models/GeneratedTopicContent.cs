namespace Application.Vocabulary.Models;

/// <summary>
/// One generated target word: the English word, its Uzbek meaning, an example, and the model's
/// raw part-of-speech tag (e.g. "verb"; parsed into the domain enum when the word is materialized).
/// </summary>
public sealed record GeneratedTopicWord(string Word, string Translation, string? ExampleSentence, string? Pos = null);

/// <summary>
/// The LLM-generated teaching content for a vocabulary topic: a short CEFR-leveled English
/// passage and the target words that appear in it (PROJECT-SPEC module 4). Produced by an
/// <see cref="Ports.IVocabularyPassageGenerator"/> and cached on the topic.
/// </summary>
public sealed record GeneratedTopicContent(string Passage, IReadOnlyList<GeneratedTopicWord> Words)
{
    /// <summary>An empty result, signalling generation was unavailable (topic stays pending).</summary>
    public static GeneratedTopicContent Empty { get; } =
        new(string.Empty, Array.Empty<GeneratedTopicWord>());

    /// <summary>True when there is a usable passage with at least one word.</summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Passage) && Words.Count > 0;
}
