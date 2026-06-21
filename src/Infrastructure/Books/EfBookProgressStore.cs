using Application.Books.Ports;
using Domain.Books;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Books;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IBookProgressStore"/>. One row per (learner, book),
/// with the per-section best scores stored as an owned collection so a learner's reading progress
/// is durable.
/// </summary>
public sealed class EfBookProgressStore : IBookProgressStore
{
    private readonly EnglishAiDbContext _db;

    public EfBookProgressStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<BookProgress?> GetAsync(Guid learnerId, Guid bookId, CancellationToken cancellationToken) =>
        await _db.BookProgress.FirstOrDefaultAsync(
            p => p.LearnerId == learnerId && p.BookId == bookId, cancellationToken);

    public async Task<IReadOnlyCollection<BookProgress>> GetByLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken) =>
        await _db.BookProgress
            .Where(p => p.LearnerId == learnerId)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(BookProgress progress, CancellationToken cancellationToken)
    {
        if (_db.Entry(progress).State == EntityState.Detached)
            await _db.BookProgress.AddAsync(progress, cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
