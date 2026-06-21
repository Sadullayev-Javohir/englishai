using Domain.Books;

namespace Application.Books.Ports;

/// <summary>
/// Persistence port for per-learner <see cref="BookProgress"/> (which sections of which books a
/// learner has passed). One record per (learner, book). Implemented by an EF Core adapter in
/// production and an in-memory adapter for dev/tests.
/// </summary>
public interface IBookProgressStore
{
    /// <summary>Returns the learner's progress through one book, or null if untouched.</summary>
    Task<BookProgress?> GetAsync(Guid learnerId, Guid bookId, CancellationToken cancellationToken);

    /// <summary>
    /// All of a learner's book-progress records - used by the library catalog so each book can show
    /// "X/Y sections read" without a per-book round trip.
    /// </summary>
    Task<IReadOnlyCollection<BookProgress>> GetByLearnerAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>Inserts or updates a progress record.</summary>
    Task SaveAsync(BookProgress progress, CancellationToken cancellationToken);

    /// <summary>
    /// Persists all tracked book-submit changes as one durable commit. EF-backed implementations
    /// share a DbContext with the learner-profile repository, so this also commits the profile
    /// activity/error observations recorded by the same quiz request. In-memory implementations
    /// are already immediately consistent and therefore use a no-op flush.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken);
}
