using Application.Backfill.Models;
using Domain.Vocabulary;

namespace Application.Backfill.Ports;

/// <summary>
/// Adapts one learning module (vocabulary, reading, grammar, listening, writing) to the content
/// backfill: it knows how to build that module's LLM request for a topic and how to parse + persist
/// the reply. Implementations reuse the module's existing Claude generator (the same system prompt,
/// user prompt and parser used on the lazy-fill path) and its repository, so batch-filled content is
/// byte-for-byte what a live first-open would have produced.
/// </summary>
public interface IContentBackfiller
{
    /// <summary>A stable, unique module key (also used as the <see cref="BatchContentRequest.CustomId"/> prefix).</summary>
    string Module { get; }

    /// <summary>
    /// Builds the request for this topic, or <c>null</c> if the topic's content for this module is
    /// already filled (idempotent - re-running the backfill only fills the gaps).
    /// </summary>
    Task<BatchContentRequest?> BuildRequestAsync(VocabularyTopic topic, CancellationToken cancellationToken);

    /// <summary>
    /// Parses the model reply and persists it to the database. Returns <c>true</c> if content was
    /// filled and saved; <c>false</c> if the reply was empty/invalid (the topic stays pending).
    /// </summary>
    Task<bool> ApplyResultAsync(VocabularyTopic topic, string responseText, CancellationToken cancellationToken);
}
