using System.Collections.Concurrent;
using Application.Writing.Ports;
using Domain.Writing;

namespace Infrastructure.Writing;

/// <summary>
/// In-memory <see cref="IWritingTaskRepository"/> for dev/tests and for running the app without
/// a database. Writing tasks are generated lazily per learning-spine topic and cached here on
/// first open, so the store starts empty (the topic catalog is the spine, not a separate seed).
/// </summary>
public sealed class InMemoryWritingTaskRepository : IWritingTaskRepository
{
    private readonly ConcurrentDictionary<Guid, WritingTask> _tasks = new();

    public Task<WritingTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_tasks.TryGetValue(id, out var task) ? task : null);

    public Task<WritingTask?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken)
    {
        var task = _tasks.Values.FirstOrDefault(t => t.VocabularyTopicId == vocabularyTopicId);
        return Task.FromResult(task);
    }

    public Task SaveAsync(WritingTask task, CancellationToken cancellationToken)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _tasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WritingTask>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<WritingTask> result = _tasks.Values
            .OrderBy(t => t.Level)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }
}
