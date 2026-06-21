using Application.Reading.Models;
using Domain.Assessment;

namespace Application.Reading.Ports;

/// <summary>
/// Generates a reading lesson for a learning-spine topic: a short, CEFR-leveled English passage,
/// an interactive glossary and comprehension questions (PROJECT-SPEC Faza 6, topic-scoped). This
/// is the sanctioned dynamic path of docs/development-guide.md rule 11 - the passage, questions and explanations
/// are the target language (English, authored teaching content), and each glossary word's Uzbek
/// meaning is a translation of a source word with structured, validated output, not free-invented
/// UI copy. Cost is controlled (rule 10) by a budget model, a cached system prompt and a capped
/// output. Implementations must return <see cref="GeneratedReadingContent.Empty"/> (never throw,
/// never fabricate) when generation is unavailable, so the lesson stays honestly "pending".
/// </summary>
public interface IReadingContentGenerator
{
    /// <summary>
    /// Generates the lesson. <paramref name="targetWords"/> are the topic's vocabulary words to reuse
    /// (the unified spine), woven in softly so the learner meets them again across skills; null/empty
    /// when the vocabulary topic has not been filled yet.
    /// </summary>
    Task<GeneratedReadingContent> GenerateAsync(
        string title,
        CefrLevel level,
        IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default);
}
