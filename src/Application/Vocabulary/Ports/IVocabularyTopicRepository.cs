using Domain.Assessment;
using Domain.Vocabulary;

namespace Application.Vocabulary.Ports;

/// <summary>
/// Persistence port for the <see cref="VocabularyTopic"/> aggregate (the curated topic catalog
/// plus its lazily-filled passages). Implemented by an EF Core adapter (PostgreSQL) in
/// production and an in-memory adapter (seeded with the catalog) for dev/tests.
/// </summary>
public interface IVocabularyTopicRepository
{
    /// <summary>Returns a topic (with its words) by id, or <c>null</c> if it does not exist.</summary>
    Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the topics matching any of the given ids in a single round-trip (unknown ids are simply
    /// absent from the result - no exception). Used to batch-resolve topics instead of looping
    /// per-id (N+1 queries).
    /// </summary>
    Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>The curated topics for a CEFR level, ordered for catalog display.</summary>
    Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken);

    /// <summary>Inserts a new topic or updates an existing one (used to cache filled content).</summary>
    Task SaveAsync(VocabularyTopic topic, CancellationToken cancellationToken);

    /// <summary>All topics, ordered for catalog display (by level, then sequence).</summary>
    Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Removes the topic with the given id, if it exists.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
