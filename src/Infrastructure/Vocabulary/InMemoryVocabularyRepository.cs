using System.Collections.Concurrent;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;

namespace Infrastructure.Vocabulary;

/// <summary>
/// In-memory <see cref="IVocabularyRepository"/> for dev/tests and for running the app
/// without a database. Stores the live aggregate instance keyed by id, so in-place
/// mutations are reflected on the next read.
/// </summary>
public sealed class InMemoryVocabularyRepository : IVocabularyRepository
{
    private readonly ConcurrentDictionary<Guid, VocabularyItem> _items = new();

    public Task<VocabularyItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task<IReadOnlyList<VocabularyItem>> GetByLearnerIdAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<VocabularyItem> result = _items.Values
            .Where(v => v.LearnerId == learnerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<VocabularyItem>> GetDueForLearnerAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        IReadOnlyList<VocabularyItem> result = _items.Values
            .Where(v => v.LearnerId == learnerId && v.IsDue(now))
            .OrderBy(v => v.Schedule.NextReviewAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<DueReviewSummary> GetDueSummaryAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = _items.Values.Where(v => v.LearnerId == learnerId && v.IsDue(now)).ToList();
        return Task.FromResult(new DueReviewSummary(
            due.Count,
            due.Where(v => v.SourceTopicId.HasValue)
                .Select(v => v.SourceTopicId)
                .Distinct()
                .Count()));
    }

    public Task<IReadOnlyList<VocabularyItem>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        IReadOnlyList<VocabularyItem> result = _items.Values
            .Where(v => v.IsDue(now))
            .OrderBy(v => v.LearnerId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(VocabularyItem item, CancellationToken cancellationToken)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _items.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
