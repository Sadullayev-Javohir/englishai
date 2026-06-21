using System.Collections.Concurrent;
using Application.Books.Ports;
using Domain.Assessment;
using Domain.Books;

namespace Infrastructure.Books;

/// <summary>
/// In-memory <see cref="IBookRepository"/> for dev/tests and for running the app without a
/// database. Seeded with the curated library so the Books module works offline; section content is
/// generated lazily and cached on the in-memory book on first open.
/// </summary>
public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly ConcurrentDictionary<Guid, Book> _books = new();

    public InMemoryBookRepository()
        : this(BookCatalogSeed.Books())
    {
    }

    // Parameter type is IReadOnlyList (not IEnumerable) so the DI container cannot satisfy it and
    // falls back to the seeded parameterless constructor.
    public InMemoryBookRepository(IReadOnlyList<Book> seed)
    {
        foreach (var book in seed)
            _books[book.Id] = book;
    }

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_books.TryGetValue(id, out var book) ? book : null);

    public Task<IReadOnlyList<Book>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken)
    {
        IReadOnlyList<Book> result = _books.Values
            .Where(b => b.Level == level)
            .OrderBy(b => b.Title)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Book> result = _books.Values
            .OrderBy(b => b.Level)
            .ThenBy(b => b.Title)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(Book book, CancellationToken cancellationToken)
    {
        _books[book.Id] = book;
        return Task.CompletedTask;
    }
}
