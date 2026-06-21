using Domain.Assessment;
using Domain.Reading;

namespace Application.Reading.Ports;

/// <summary>
/// Persistence port for the <see cref="ReadingPassage"/> aggregate. Implemented by an EF
/// Core adapter (PostgreSQL) in production and an in-memory adapter (seeded with the
/// curated catalog) for dev/tests.
/// </summary>
public interface IReadingRepository
{
    /// <summary>Returns a single passage by id, or <c>null</c> if it does not exist.</summary>
    Task<ReadingPassage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the reading lesson bound to a learning-spine topic, or <c>null</c> if none has been
    /// created yet (the topic-scoped lazy-fill path: the caller generates and saves it on first open).
    /// </summary>
    Task<ReadingPassage?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken);

    /// <summary>
    /// Passages whose CEFR band is within <paramref name="levelTolerance"/> of
    /// <paramref name="level"/>, ordered by closeness to the learner's level - the same
    /// adaptive curation used by the video catalog (PROJECT-SPEC Faza 6).
    /// </summary>
    Task<IReadOnlyList<ReadingPassage>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken);

    /// <summary>Inserts a new passage or updates an existing one.</summary>
    Task SaveAsync(ReadingPassage passage, CancellationToken cancellationToken);

    /// <summary>All passages, ordered for admin catalog display (by level, then created desc).</summary>
    Task<IReadOnlyList<ReadingPassage>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Removes the passage with the given id, if it exists.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
