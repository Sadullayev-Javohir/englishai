using System.Collections.Concurrent;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;

namespace Infrastructure.Vocabulary;

/// <summary>
/// Dev/test <see cref="ITopicCompletionStore"/> - a process-local map of (learner, topic) completion
/// records. Resets on restart; the EF adapter is the durable production path.
/// </summary>
public sealed class InMemoryTopicCompletionStore : ITopicCompletionStore
{
    private readonly ConcurrentDictionary<(Guid Learner, Guid Topic), TopicCompletionRecord> _records = new();

    public Task<TopicCompletionRecord?> GetAsync(
        Guid learnerId, Guid vocabularyTopicId, CancellationToken cancellationToken)
    {
        _records.TryGetValue((learnerId, vocabularyTopicId), out var record);
        return Task.FromResult(record);
    }

    public Task SaveAsync(TopicCompletionRecord record, CancellationToken cancellationToken)
    {
        _records[(record.LearnerId, record.VocabularyTopicId)] = record;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Guid>> GetMasteredTopicIdsAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid> mastered = _records.Values
            .Where(r => r.LearnerId == learnerId && r.IsMastered)
            .Select(r => r.VocabularyTopicId)
            .ToList();
        return Task.FromResult(mastered);
    }

    public Task<IReadOnlyCollection<TopicCompletionRecord>> GetByLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TopicCompletionRecord> records = _records.Values
            .Where(r => r.LearnerId == learnerId)
            .ToList();
        return Task.FromResult(records);
    }

    public Task<IReadOnlyCollection<TopicCompletionRecord>> GetWithVocabularyProgressAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TopicCompletionRecord> records = _records.Values
            .Where(record => record.ScoreFor(Domain.Learning.SkillType.Vocabulary) > 0)
            .ToList();
        return Task.FromResult(records);
    }
}
