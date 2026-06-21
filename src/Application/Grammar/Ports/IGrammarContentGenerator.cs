using Application.Grammar.Models;
using Domain.Assessment;

namespace Application.Grammar.Ports;

/// <summary>
/// Generates a 5-step grammar lesson for a learning-spine topic's grammar focus, taught in that
/// topic's own context (PROJECT-SPEC G.2, topic-scoped): a contextual English intro, an English rule
/// explanation, recognition + active-use exercises and Speaking/Writing application tasks. This is
/// the sanctioned dynamic path of docs/development-guide.md rule 11 - everything is the target language (English
/// teaching content) with structured, validated output. Cost is controlled (rule 10) by a budget
/// model, a cached system prompt and a capped output. Implementations must return
/// <see cref="GeneratedGrammarContent.Empty"/> (never throw, never fabricate) when generation is
/// unavailable, so the lesson stays honestly "pending".
/// </summary>
public interface IGrammarContentGenerator
{
    /// <summary>
    /// Generates the lesson. <paramref name="targetWords"/> are the topic's vocabulary words to reuse
    /// (the unified spine), woven in softly; null/empty when the vocabulary topic is not filled yet.
    /// </summary>
    Task<GeneratedGrammarContent> GenerateAsync(
        string topicTitle,
        string grammarFocusCode,
        CefrLevel level,
        IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default);
}
