using Application.Books.Ports;
using Domain.Assessment;
using Domain.Books;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Books;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IBookRepository"/>. The owned sections and their
/// questions load and persist with each book (owned collections are always included).
/// </summary>
public sealed class EfBookRepository : IBookRepository
{
    private readonly EnglishAiDbContext _db;

    public EfBookRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Book>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken) =>
        await _db.Books
            .Where(b => b.Level == level)
            .OrderBy(b => b.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.Books
            .OrderBy(b => b.Level)
            .ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(Book book, CancellationToken cancellationToken)
    {
        if (_db.Entry(book).State == EntityState.Detached)
            await _db.Books.AddAsync(book, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
