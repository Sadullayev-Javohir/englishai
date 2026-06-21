using Application.Writing.Ports;
using Domain.Writing;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Writing;

/// <summary>EF Core / PostgreSQL adapter for <see cref="IWritingTaskRepository"/>.</summary>
public sealed class EfWritingTaskRepository : IWritingTaskRepository
{
    private readonly EnglishAiDbContext _db;

    public EfWritingTaskRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<WritingTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.WritingTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<WritingTask?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken) =>
        await _db.WritingTasks.FirstOrDefaultAsync(
            t => t.VocabularyTopicId == vocabularyTopicId, cancellationToken);

    public async Task SaveAsync(WritingTask task, CancellationToken cancellationToken)
    {
        if (_db.Entry(task).State == EntityState.Detached)
            await _db.WritingTasks.AddAsync(task, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await _db.WritingTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
            return;

        _db.WritingTasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WritingTask>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.WritingTasks
            .OrderBy(t => t.Level)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
}
