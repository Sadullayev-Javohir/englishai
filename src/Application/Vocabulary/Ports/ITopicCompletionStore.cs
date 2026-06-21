using Domain.Vocabulary;

namespace Application.Vocabulary.Ports;

/// <summary>
/// Persistence port for <see cref="TopicCompletionRecord"/> - a learner's per-module progress toward
/// mastering each vocabulary topic across all six modules (PROJECT-SPEC K.5). EF Core adapter in
/// production, in-memory in dev/tests (docs/development-guide.md rule 10 - external services behind ports).
/// </summary>
public interface ITopicCompletionStore
{
    /// <summary>Returns the learner's record for a topic, or <c>null</c> if they have not started it.</summary>
    Task<TopicCompletionRecord?> GetAsync(Guid learnerId, Guid vocabularyTopicId, CancellationToken cancellationToken);

    /// <summary>Inserts a new record or updates an existing one, committing immediately.</summary>
    Task SaveAsync(TopicCompletionRecord record, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a record change without committing it, so it can be persisted in the SAME commit as
    /// another aggregate the request also mutated (typically the learner profile).
    ///
    /// A submit handler that commits two aggregates separately can fail between them: the first is
    /// already durable, the learner gets a 500, and a retry re-applies work that partly landed.
    /// Adapters that are immediately consistent (in-memory) fall back to <see cref="SaveAsync"/>.
    /// </summary>
    Task TrackAsync(TopicCompletionRecord record, CancellationToken cancellationToken) =>
        SaveAsync(record, cancellationToken);

    /// <summary>
    /// Commits everything staged on this request as one durable write. EF-backed implementations
    /// share a DbContext with the learner-profile repository, so this commits both. In-memory
    /// implementations have nothing to flush.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>The ids of every topic the learner has fully mastered (all six modules passed).</summary>
    Task<IReadOnlyCollection<Guid>> GetMasteredTopicIdsAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Every completion record the learner has started, across all topics and levels. Used by the
    /// Level Map / roadmap to show per-topic module progress in a single page load.
    /// </summary>
    Task<IReadOnlyCollection<TopicCompletionRecord>> GetByLearnerAsync(Guid learnerId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TopicCompletionRecord>> GetWithVocabularyProgressAsync(
        CancellationToken cancellationToken);
}
