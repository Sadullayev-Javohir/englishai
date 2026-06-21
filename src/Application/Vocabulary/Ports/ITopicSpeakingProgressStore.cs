using Domain.Vocabulary;

namespace Application.Vocabulary.Ports;

/// <summary>
/// Persistence port for <see cref="TopicSpeakingProgress"/> - a learner's speaking practice toward
/// learning each vocabulary topic (the 5-minute rule). EF Core adapter in production, in-memory in
/// dev/tests (docs/development-guide.md rule 10 - external services behind ports).
/// </summary>
public interface ITopicSpeakingProgressStore
{
    /// <summary>Returns the learner's progress for a topic, or <c>null</c> if they have not started it.</summary>
    Task<TopicSpeakingProgress?> GetAsync(Guid learnerId, Guid vocabularyTopicId, CancellationToken cancellationToken);

    /// <summary>Inserts a new progress record or updates an existing one, committing immediately.</summary>
    Task SaveAsync(TopicSpeakingProgress progress, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a change without committing it, so speaking progress and the topic completion record
    /// it feeds land in one commit rather than two - a failure between them would otherwise leave a
    /// learner credited for practice on one row and not the other. Adapters that are immediately
    /// consistent (in-memory) fall back to <see cref="SaveAsync"/>.
    /// </summary>
    Task TrackAsync(TopicSpeakingProgress progress, CancellationToken cancellationToken) =>
        SaveAsync(progress, cancellationToken);

    /// <summary>The ids of every topic the learner has learned (spoken about for 5+ minutes).</summary>
    Task<IReadOnlyCollection<Guid>> GetLearnedTopicIdsAsync(Guid learnerId, CancellationToken cancellationToken);
}
