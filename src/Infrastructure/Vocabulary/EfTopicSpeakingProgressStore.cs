using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="ITopicSpeakingProgressStore"/>. One row per
/// (learner, topic); a learner's speaking practice toward learning each topic is durable.
/// </summary>
public sealed class EfTopicSpeakingProgressStore : ITopicSpeakingProgressStore
{
    private readonly EnglishAiDbContext _db;

    public EfTopicSpeakingProgressStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<TopicSpeakingProgress?> GetAsync(
        Guid learnerId, Guid vocabularyTopicId, CancellationToken cancellationToken) =>
        await _db.TopicSpeakingProgress.FirstOrDefaultAsync(
            p => p.LearnerId == learnerId && p.VocabularyTopicId == vocabularyTopicId, cancellationToken);

    public async Task SaveAsync(TopicSpeakingProgress progress, CancellationToken cancellationToken)
    {
        await TrackAsync(progress, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackAsync(TopicSpeakingProgress progress, CancellationToken cancellationToken)
    {
        // A freshly created record is detached; a loaded one is already tracked.
        if (_db.Entry(progress).State == EntityState.Detached)
            await _db.TopicSpeakingProgress.AddAsync(progress, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetLearnedTopicIdsAsync(
        Guid learnerId, CancellationToken cancellationToken) =>
        await _db.TopicSpeakingProgress
            .Where(p => p.LearnerId == learnerId && p.LearnedAt != null)
            .Select(p => p.VocabularyTopicId)
            .ToListAsync(cancellationToken);
}
