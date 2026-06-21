using Domain.Assessment;
using Domain.Listening;

namespace Application.Listening.Ports;

/// <summary>
/// Persistence port for the <see cref="ListeningExercise"/> aggregate. Implemented by an EF
/// Core adapter (PostgreSQL) in production and an in-memory adapter (seeded with the curated
/// catalog) for dev/tests.
/// </summary>
public interface IListeningRepository
{
    /// <summary>Returns a single exercise by id, or <c>null</c> if it does not exist.</summary>
    Task<ListeningExercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the exercise generated for a learning-spine topic (the topic-scoped lazy-fill path),
    /// or <c>null</c> if none has been generated yet.
    /// </summary>
    Task<ListeningExercise?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken);

    /// <summary>
    /// Exercises whose CEFR band is within <paramref name="levelTolerance"/> of
    /// <paramref name="level"/>, ordered by closeness to the learner's level - the same
    /// adaptive curation used by the reading and video catalogs (PROJECT-SPEC Faza 4/6).
    /// </summary>
    Task<IReadOnlyList<ListeningExercise>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken);

    /// <summary>
    /// The whole curated catalog ordered easiest-first (CEFR level ascending), so the learner can
    /// browse every level as one progressively harder list (PROJECT-SPEC Faza 4 - leveled audio).
    /// </summary>
    Task<IReadOnlyList<ListeningExercise>> GetAllOrderedByLevelAsync(CancellationToken cancellationToken);

    /// <summary>Inserts a new exercise or updates an existing one.</summary>
    Task SaveAsync(ListeningExercise exercise, CancellationToken cancellationToken);

    /// <summary>Removes the exercise with the given id, if it exists.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
