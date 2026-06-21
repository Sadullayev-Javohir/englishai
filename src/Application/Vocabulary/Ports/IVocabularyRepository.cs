using Domain.Vocabulary;

namespace Application.Vocabulary.Ports;

public sealed record DueReviewSummary(int ItemCount, int TopicCount);

/// <summary>
/// Persistence port for the <see cref="VocabularyItem"/> aggregate. Implemented by an
/// EF Core adapter (PostgreSQL) in production and an in-memory adapter for dev/tests.
/// </summary>
public interface IVocabularyRepository
{
    /// <summary>Returns a single item by id, or <c>null</c> if it does not exist.</summary>
    Task<VocabularyItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>All vocabulary items a learner is studying, newest first.</summary>
    Task<IReadOnlyList<VocabularyItem>> GetByLearnerIdAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>Items the given learner is due to review as of <paramref name="now"/>.</summary>
    Task<IReadOnlyList<VocabularyItem>> GetDueForLearnerAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<DueReviewSummary> GetDueSummaryAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Every due item across all learners, for the daily notification job. Grouped by
    /// learner downstream so each learner gets at most one reminder (PROJECT-SPEC #4).
    /// </summary>
    Task<IReadOnlyList<VocabularyItem>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Inserts a new item or updates an existing one.</summary>
    Task SaveAsync(VocabularyItem item, CancellationToken cancellationToken);

    /// <summary>Removes one saved vocabulary item.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
