using System.Collections.Concurrent;
using Application.Listening.Ports;
using Domain.Assessment;
using Domain.Listening;

namespace Infrastructure.Listening;

/// <summary>
/// In-memory <see cref="IListeningRepository"/> for dev/tests and for running the app without
/// a database. Seeded with the curated catalog (<see cref="ListeningCatalogSeed"/>) so the
/// catalog/exercise/quiz endpoints have content to serve.
/// </summary>
public sealed class InMemoryListeningRepository : IListeningRepository
{
    private readonly ConcurrentDictionary<Guid, ListeningExercise> _exercises = new();

    public InMemoryListeningRepository()
        : this(ListeningCatalogSeed.Exercises())
    {
    }

    // Parameter type is IReadOnlyList (not IEnumerable) so the DI container cannot satisfy it
    // and falls back to the seeded parameterless constructor.
    public InMemoryListeningRepository(IReadOnlyList<ListeningExercise> seed)
    {
        foreach (var exercise in seed)
            _exercises[exercise.Id] = exercise;
    }

    public Task<ListeningExercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_exercises.TryGetValue(id, out var exercise) ? exercise : null);

    public Task<ListeningExercise?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken) =>
        Task.FromResult(_exercises.Values.FirstOrDefault(e => e.VocabularyTopicId == vocabularyTopicId));

    public Task<IReadOnlyList<ListeningExercise>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken)
    {
        IReadOnlyList<ListeningExercise> result = _exercises.Values
            .Where(e => Math.Abs((int)e.Level - (int)level) <= levelTolerance)
            .OrderBy(e => Math.Abs((int)e.Level - (int)level))
            .ThenByDescending(e => e.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ListeningExercise>> GetAllOrderedByLevelAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ListeningExercise> result = _exercises.Values
            .OrderBy(e => (int)e.Level)
            .ThenByDescending(e => e.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(ListeningExercise exercise, CancellationToken cancellationToken)
    {
        _exercises[exercise.Id] = exercise;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _exercises.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
