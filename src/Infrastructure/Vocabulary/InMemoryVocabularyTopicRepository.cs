using System.Collections.Concurrent;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;

namespace Infrastructure.Vocabulary;

/// <summary>
/// In-memory <see cref="IVocabularyTopicRepository"/> for dev/tests and for running the app without
/// a database. Seeded once with the curated catalog (300 topics) at construction; stores the live
/// aggregate instance keyed by id, so a lazily-filled passage is cached for the next read.
/// </summary>
public sealed class InMemoryVocabularyTopicRepository : IVocabularyTopicRepository
{
    private readonly ConcurrentDictionary<Guid, VocabularyTopic> _topics = new();

    public InMemoryVocabularyTopicRepository()
    {
        foreach (var topic in VocabularyTopicCatalog.Topics())
            _topics[topic.Id] = topic;
    }

    public Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_topics.TryGetValue(id, out var topic) ? topic : null);

    public Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<VocabularyTopic> result = ids
            .Select(id => _topics.TryGetValue(id, out var topic) ? topic : null)
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(
        CefrLevel level, CancellationToken cancellationToken)
    {
        IReadOnlyList<VocabularyTopic> result = _topics.Values
            .Where(t => t.Level == level)
            // Learning order: the level's grammar progression, shared by every skill's catalog.
            .OrderBy(t => t.Sequence)
            .ThenBy(t => t.Title)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        _topics[topic.Id] = topic;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<VocabularyTopic> result = _topics.Values
            .OrderBy(t => t.Level)
            .ThenBy(t => t.Sequence)
            .ToList();
        return Task.FromResult(result);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _topics.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
