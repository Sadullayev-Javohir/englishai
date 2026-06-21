using Domain.Assessment;
using Domain.Grammar;

namespace Application.Grammar.Ports;

/// <summary>
/// Persistence port for the <see cref="GrammarLesson"/> aggregate. Implemented by an EF
/// Core adapter (PostgreSQL) in production and an in-memory adapter (seeded with the
/// curated catalog) for dev/tests.
/// </summary>
public interface IGrammarRepository
{
    /// <summary>Returns a single lesson by id, or <c>null</c> if it does not exist.</summary>
    Task<GrammarLesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the lessons matching any of the given ids in a single round-trip (unknown ids are
    /// simply absent from the result - no exception). Used to batch-resolve lessons instead of
    /// looping per-id (N+1 queries).
    /// </summary>
    Task<IReadOnlyList<GrammarLesson>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the grammar lesson bound to a learning-spine topic, or <c>null</c> if none has been
    /// created yet (the topic-scoped lazy-fill path: the caller generates and saves it on first open).
    /// </summary>
    Task<GrammarLesson?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken);

    /// <summary>
    /// Lessons whose CEFR band is within <paramref name="levelTolerance"/> of
    /// <paramref name="level"/>, ordered first by the Uzbek-learner difficulty priority
    /// (<see cref="GrammarLesson.Category"/>) and then by closeness to the learner's level
    /// (PROJECT-SPEC G.2 priority ordering).
    /// </summary>
    Task<IReadOnlyList<GrammarLesson>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken);

    /// <summary>Inserts a new lesson or updates an existing one.</summary>
    Task SaveAsync(GrammarLesson lesson, CancellationToken cancellationToken);

    /// <summary>All lessons, ordered for catalog display (by level, then newest first).</summary>
    Task<IReadOnlyList<GrammarLesson>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Removes the lesson with the given id, if it exists.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
