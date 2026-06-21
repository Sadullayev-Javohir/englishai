using System.Collections.Concurrent;
using Application.Reading.Ports;
using Domain.Assessment;
using Domain.Reading;

namespace Infrastructure.Reading;

/// <summary>
/// In-memory <see cref="IReadingRepository"/> for dev/tests and for running the app without
/// a database. Reading lessons are generated lazily per learning-spine topic and cached here on
/// first open, so the store starts empty (the topic catalog is the spine, not a separate seed).
/// </summary>
public sealed class InMemoryReadingRepository : IReadingRepository
{
    private readonly ConcurrentDictionary<Guid, ReadingPassage> _passages = new();

    public InMemoryReadingRepository()
        : this(Array.Empty<ReadingPassage>())
    {
    }

    // Parameter type is IReadOnlyList (not IEnumerable) so the DI container cannot satisfy
    // it and falls back to the empty parameterless constructor.
    public InMemoryReadingRepository(IReadOnlyList<ReadingPassage> seed)
    {
        foreach (var passage in seed)
            _passages[passage.Id] = passage;
    }

    public Task<ReadingPassage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_passages.TryGetValue(id, out var passage) ? passage : null);

    public Task<ReadingPassage?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken)
    {
        var passage = _passages.Values.FirstOrDefault(p => p.VocabularyTopicId == vocabularyTopicId);
        return Task.FromResult(passage);
    }

    public Task<IReadOnlyList<ReadingPassage>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReadingPassage> result = _passages.Values
            .Where(p => Math.Abs((int)p.Level - (int)level) <= levelTolerance)
            .OrderBy(p => Math.Abs((int)p.Level - (int)level))
            .ThenByDescending(p => p.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(ReadingPassage passage, CancellationToken cancellationToken)
    {
        _passages[passage.Id] = passage;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReadingPassage>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReadingPassage> result = _passages.Values
            .OrderBy(p => p.Level)
            .ThenByDescending(p => p.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _passages.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
