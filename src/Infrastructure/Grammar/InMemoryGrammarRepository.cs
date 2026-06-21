using System.Collections.Concurrent;
using Application.Grammar.Ports;
using Domain.Assessment;
using Domain.Grammar;

namespace Infrastructure.Grammar;

/// <summary>
/// In-memory <see cref="IGrammarRepository"/> for dev/tests and for running the app without a
/// database. Grammar lessons are generated lazily per learning-spine topic and cached here on first
/// open, so the store starts empty (the topic catalog is the spine, not a separate seed).
/// </summary>
public sealed class InMemoryGrammarRepository : IGrammarRepository
{
    private readonly ConcurrentDictionary<Guid, GrammarLesson> _lessons = new();

    public InMemoryGrammarRepository()
        : this(Array.Empty<GrammarLesson>())
    {
    }

    // Parameter type is IReadOnlyList (not IEnumerable) so the DI container cannot satisfy
    // it and falls back to the empty parameterless constructor.
    public InMemoryGrammarRepository(IReadOnlyList<GrammarLesson> seed)
    {
        foreach (var lesson in seed)
            _lessons[lesson.Id] = lesson;
    }

    public Task<GrammarLesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_lessons.TryGetValue(id, out var lesson) ? lesson : null);

    public Task<IReadOnlyList<GrammarLesson>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<GrammarLesson> result = ids
            .Select(id => _lessons.TryGetValue(id, out var lesson) ? lesson : null)
            .Where(l => l is not null)
            .Select(l => l!)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<GrammarLesson?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken)
    {
        var lesson = _lessons.Values.FirstOrDefault(l => l.VocabularyTopicId == vocabularyTopicId);
        return Task.FromResult(lesson);
    }

    public Task<IReadOnlyList<GrammarLesson>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken)
    {
        IReadOnlyList<GrammarLesson> result = _lessons.Values
            .Where(l => Math.Abs((int)l.Level - (int)level) <= levelTolerance)
            .OrderBy(l => (int)l.Category)
            .ThenBy(l => Math.Abs((int)l.Level - (int)level))
            .ThenByDescending(l => l.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(GrammarLesson lesson, CancellationToken cancellationToken)
    {
        _lessons[lesson.Id] = lesson;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GrammarLesson>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<GrammarLesson> result = _lessons.Values
            .OrderBy(l => l.Level)
            .ThenByDescending(l => l.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _lessons.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
