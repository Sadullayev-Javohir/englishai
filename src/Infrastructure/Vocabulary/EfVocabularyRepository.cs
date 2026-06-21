using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IVocabularyRepository"/>. The owned
/// <see cref="ReviewSchedule"/> loads and persists inline with each item.
/// </summary>
public sealed class EfVocabularyRepository : IVocabularyRepository
{
    private readonly EnglishAiDbContext _db;

    public EfVocabularyRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public Task<VocabularyItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.VocabularyItems.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VocabularyItem>> GetByLearnerIdAsync(
        Guid learnerId, CancellationToken cancellationToken) =>
        await _db.VocabularyItems
            .AsNoTracking()
            .Where(v => v.LearnerId == learnerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VocabularyItem>> GetDueForLearnerAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await _db.VocabularyItems
            .AsNoTracking()
            .Where(v => v.LearnerId == learnerId
                        && v.Schedule.Stage != ReviewStage.Mastered
                        && v.Schedule.NextReviewAt != null
                        && v.Schedule.NextReviewAt <= now)
            .OrderBy(v => v.Schedule.NextReviewAt)
            .ToListAsync(cancellationToken);

    public async Task<DueReviewSummary> GetDueSummaryAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = _db.VocabularyItems
            .AsNoTracking()
            .Where(v => v.LearnerId == learnerId
                        && v.Schedule.Stage != ReviewStage.Mastered
                        && v.Schedule.NextReviewAt != null
                        && v.Schedule.NextReviewAt <= now);

        var summary = await due
            .GroupBy(_ => 1)
            .Select(group => new
            {
                ItemCount = group.Count(),
                TopicCount = group.Where(v => v.SourceTopicId != null)
                    .Select(v => v.SourceTopicId)
                    .Distinct()
                    .Count(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return summary is null
            ? new DueReviewSummary(0, 0)
            : new DueReviewSummary(summary.ItemCount, summary.TopicCount);
    }

    public async Task<IReadOnlyList<VocabularyItem>> GetDueAsync(
        DateTimeOffset now, CancellationToken cancellationToken) =>
        await _db.VocabularyItems
            .AsNoTracking()
            .Where(v => v.Schedule.Stage != ReviewStage.Mastered
                        && v.Schedule.NextReviewAt != null
                        && v.Schedule.NextReviewAt <= now)
            .OrderBy(v => v.LearnerId)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(VocabularyItem item, CancellationToken cancellationToken)
    {
        if (_db.Entry(item).State == EntityState.Detached)
            await _db.VocabularyItems.AddAsync(item, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _db.VocabularyItems.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (item is null) return;

        _db.VocabularyItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
