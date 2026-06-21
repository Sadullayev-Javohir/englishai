using System.Collections.Concurrent;
using Application.Books.Ports;
using Domain.Books;

namespace Infrastructure.Books;

/// <summary>
/// In-memory <see cref="IBookProgressStore"/> for dev/tests and for running the app without a
/// database. Keyed by (learner, book).
/// </summary>
public sealed class InMemoryBookProgressStore : IBookProgressStore
{
    private readonly ConcurrentDictionary<(Guid Learner, Guid Book), BookProgress> _progress = new();

    public Task<BookProgress?> GetAsync(Guid learnerId, Guid bookId, CancellationToken cancellationToken) =>
        Task.FromResult(_progress.TryGetValue((learnerId, bookId), out var progress) ? progress : null);

    public Task<IReadOnlyCollection<BookProgress>> GetByLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BookProgress> result = _progress.Values
            .Where(p => p.LearnerId == learnerId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(BookProgress progress, CancellationToken cancellationToken)
    {
        _progress[(progress.LearnerId, progress.BookId)] = progress;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
