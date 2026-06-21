using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IVocabularyTopicRepository"/>. The owned target-word
/// collection loads and persists with each topic, so a lazily-filled passage is cached durably.
/// </summary>
public sealed class EfVocabularyTopicRepository : IVocabularyTopicRepository
{
    private readonly EnglishAiDbContext _db;

    public EfVocabularyTopicRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.VocabularyTopics.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await _db.VocabularyTopics
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(
        CefrLevel level, CancellationToken cancellationToken) =>
        await _db.VocabularyTopics
            .Where(t => t.Level == level)
            // Learning order: the level's grammar progression, shared by every skill's catalog.
            .OrderBy(t => t.Sequence)
            .ThenBy(t => t.Title)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        if (_db.Entry(topic).State == EntityState.Detached)
            await _db.VocabularyTopics.AddAsync(topic, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.VocabularyTopics
            .OrderBy(t => t.Level)
            .ThenBy(t => t.Sequence)
            .ToListAsync(cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var topic = await _db.VocabularyTopics.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (topic is null)
            return;

        _db.VocabularyTopics.Remove(topic);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
