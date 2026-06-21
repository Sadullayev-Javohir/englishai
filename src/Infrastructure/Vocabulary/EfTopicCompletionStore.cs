using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="ITopicCompletionStore"/>. One row per (learner, topic),
/// with the per-module scores stored as an owned collection so a learner's topic mastery is durable.
/// </summary>
public sealed class EfTopicCompletionStore : ITopicCompletionStore
{
    private readonly EnglishAiDbContext _db;

    public EfTopicCompletionStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<TopicCompletionRecord?> GetAsync(
        Guid learnerId, Guid vocabularyTopicId, CancellationToken cancellationToken) =>
        await _db.TopicCompletionRecords.FirstOrDefaultAsync(
            r => r.LearnerId == learnerId && r.VocabularyTopicId == vocabularyTopicId, cancellationToken);

    public async Task SaveAsync(TopicCompletionRecord record, CancellationToken cancellationToken)
    {
        await TrackAsync(record, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackAsync(TopicCompletionRecord record, CancellationToken cancellationToken)
    {
        // A freshly created record is detached; a loaded one is already tracked.
        if (_db.Entry(record).State == EntityState.Detached)
            await _db.TopicCompletionRecords.AddAsync(record, cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> GetMasteredTopicIdsAsync(
        Guid learnerId, CancellationToken cancellationToken) =>
        await _db.TopicCompletionRecords
            .Where(r => r.LearnerId == learnerId && r.MasteredAt != null)
            .Select(r => r.VocabularyTopicId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<TopicCompletionRecord>> GetByLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken) =>
        await _db.TopicCompletionRecords
            .Where(r => r.LearnerId == learnerId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<TopicCompletionRecord>> GetWithVocabularyProgressAsync(
        CancellationToken cancellationToken) =>
        await _db.TopicCompletionRecords
            .Where(record => record.ModuleScores.Any(score => score.Module == Domain.Learning.SkillType.Vocabulary))
            .ToListAsync(cancellationToken);
}
