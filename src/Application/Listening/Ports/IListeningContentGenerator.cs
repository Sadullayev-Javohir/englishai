using Application.Listening.Models;
using Domain.Assessment;

namespace Application.Listening.Ports;

/// <summary>
/// Generates a listening exercise for a learning-spine topic: a short, CEFR-leveled English
/// transcript (the spoken script) and comprehension questions (PROJECT-SPEC Faza 4, topic-scoped).
/// This is the sanctioned dynamic path of docs/development-guide.md rule 11 - the transcript, questions and
/// explanations are the target language (English, authored teaching content), with structured,
/// validated output, not free-invented Uzbek UI copy. Cost is controlled (rule 10) by a budget
/// model, a cached system prompt and a capped output. Implementations must return
/// <see cref="GeneratedListeningContent.Empty"/> (never throw, never fabricate) when generation is
/// unavailable, so the exercise stays honestly "pending".
/// </summary>
public interface IListeningContentGenerator
{
    /// <summary>
    /// Generates the exercise. <paramref name="targetWords"/> are the topic's vocabulary words to
    /// reuse (the unified spine), woven into the transcript softly; null/empty when the vocabulary
    /// topic has not been filled yet.
    /// </summary>
    Task<GeneratedListeningContent> GenerateAsync(
        string title,
        CefrLevel level,
        IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default);
}
