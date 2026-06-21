using Domain.Assessment;
using Domain.Books;

namespace Application.Books.Ports;

/// <summary>
/// Persistence port for the <see cref="Book"/> aggregate (library + sections + questions).
/// Implemented by an EF Core adapter (PostgreSQL) in production and an in-memory adapter
/// (seeded with the curated library) for dev/tests.
/// </summary>
public interface IBookRepository
{
    /// <summary>Returns a single book (with its sections and questions) by id, or null.</summary>
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>All books for one CEFR level, ordered by title.</summary>
    Task<IReadOnlyList<Book>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken);

    /// <summary>The whole library across every level, easiest-first then by title.</summary>
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Inserts a new book or updates an existing one (sections persist with it).</summary>
    Task SaveAsync(Book book, CancellationToken cancellationToken);
}
