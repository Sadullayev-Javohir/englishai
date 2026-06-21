using Application.Writing.Models;
using Domain.Assessment;

namespace Application.Writing.Ports;

/// <summary>
/// Generates a writing task for a learning-spine topic: a CEFR-leveled English prompt (a concrete
/// writing scenario about the topic) and a few short English guidance hints (PROJECT-SPEC G.3,
/// topic-scoped). This is the sanctioned dynamic path of docs/development-guide.md rule 11 - the prompt and hints
/// are the target language (English, authored teaching content), structured and validated, not
/// free-invented Uzbek UI copy. Cost is controlled (rule 10) by a budget model, a cached system
/// prompt and a capped output. Implementations must return <see cref="GeneratedWritingPrompt.Empty"/>
/// (never throw, never fabricate) when generation is unavailable, so the task stays honestly "pending".
/// </summary>
public interface IWritingPromptGenerator
{
    /// <summary>
    /// Generates the writing task. <paramref name="targetWords"/> are the topic's vocabulary words to
    /// reuse (the unified spine), suggested softly; null/empty when the vocabulary topic isn't filled.
    /// </summary>
    Task<GeneratedWritingPrompt> GenerateAsync(
        string title,
        CefrLevel level,
        IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default);
}
