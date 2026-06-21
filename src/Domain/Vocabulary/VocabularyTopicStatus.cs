namespace Domain.Vocabulary;

/// <summary>
/// Whether a <see cref="VocabularyTopic"/>'s teaching content (its passage and target
/// words) has been generated yet. Topics are seeded as metadata-only (<see cref="Pending"/>)
/// and have their passage lazily filled by the LLM on first open, then cached
/// (<see cref="Filled"/>) - the same pattern as video transcript fill (rules 8, 10).
/// </summary>
public enum VocabularyTopicStatus
{
    /// <summary>Catalog metadata only; the passage/words have not been generated yet.</summary>
    Pending = 0,

    /// <summary>The passage and its target words have been generated and cached.</summary>
    Filled = 1
}
