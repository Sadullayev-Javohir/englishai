using Domain.Writing;

namespace Application.Writing.Ports;

/// <summary>
/// Persistence port for the <see cref="WritingTask"/> aggregate. Writing tasks are now generated
/// lazily per learning-spine topic and cached, so the store starts empty (the topic catalog is the
/// spine). Implemented by an EF Core adapter (PostgreSQL) in production and an in-memory adapter for
/// dev/tests.
/// </summary>
public interface IWritingTaskRepository
{
    /// <summary>Returns a single task by id, or <c>null</c> if it does not exist.</summary>
    Task<WritingTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the writing task bound to a learning-spine topic, or <c>null</c> if none has been
    /// created yet (the topic-scoped lazy-fill path: the caller generates and saves it on first open).
    /// </summary>
    Task<WritingTask?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken);

    /// <summary>Inserts a new task or updates an existing one.</summary>
    Task SaveAsync(WritingTask task, CancellationToken cancellationToken);

    /// <summary>Returns every writing task, or an empty list if none exist.</summary>
    Task<IReadOnlyList<WritingTask>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Deletes the task with the given id, if it exists.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
