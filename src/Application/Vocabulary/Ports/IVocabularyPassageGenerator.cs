using Application.Vocabulary.Models;
using Domain.Assessment;

namespace Application.Vocabulary.Ports;

/// <summary>
/// Generates a vocabulary topic's teaching content: a short, CEFR-leveled English passage and
/// the target words it teaches (PROJECT-SPEC module 4). This is the sanctioned dynamic path of
/// docs/development-guide.md rule 11 - the passage is the target language (English), and each word's Uzbek
/// meaning is a translation of a source word with structured, validated output, not free-invented
/// UI copy. Cost is controlled (rule 10) by a budget model, a cached system prompt and a capped
/// output. Implementations must return <see cref="GeneratedTopicContent.Empty"/> (never throw,
/// never fabricate) when generation is unavailable or fails, so the topic stays honestly
/// "pending" rather than showing fake content (rules 8, 11).
/// </summary>
public interface IVocabularyPassageGenerator
{
    Task<GeneratedTopicContent> GenerateAsync(
        string title,
        CefrLevel level,
        int targetWordCount,
        CancellationToken cancellationToken = default);
}
