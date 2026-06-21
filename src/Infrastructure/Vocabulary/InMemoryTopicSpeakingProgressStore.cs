using System.Collections.Concurrent;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;

namespace Infrastructure.Vocabulary;

/// <summary>
/// Dev/test <see cref="ITopicSpeakingProgressStore"/> - a process-local map of (learner, topic)
/// speaking progress. Resets on restart; the EF adapter is the durable production path.
/// </summary>
public sealed class InMemoryTopicSpeakingProgressStore : ITopicSpeakingProgressStore
{
    private readonly ConcurrentDictionary<(Guid Learner, Guid Topic), TopicSpeakingProgress> _progress = new();

    public Task<TopicSpeakingProgress?> GetAsync(
        Guid learnerId, Guid vocabularyTopicId, CancellationToken cancellationToken)
    {
        _progress.TryGetValue((learnerId, vocabularyTopicId), out var progress);
        return Task.FromResult(progress);
    }

    public Task SaveAsync(TopicSpeakingProgress progress, CancellationToken cancellationToken)
    {
        _progress[(progress.LearnerId, progress.VocabularyTopicId)] = progress;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Guid>> GetLearnedTopicIdsAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid> learned = _progress.Values
            .Where(p => p.LearnerId == learnerId && p.IsLearned)
            .Select(p => p.VocabularyTopicId)
            .ToList();
        return Task.FromResult(learned);
    }
}
