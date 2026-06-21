using Application.Reading.Ports;
using Domain.Assessment;
using Domain.Reading;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reading;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IReadingRepository"/>. The owned glossary
/// and question collections load and persist with each passage.
/// </summary>
public sealed class EfReadingRepository : IReadingRepository
{
    private readonly EnglishAiDbContext _db;

    public EfReadingRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<ReadingPassage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.ReadingPassages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<ReadingPassage?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken) =>
        await _db.ReadingPassages.FirstOrDefaultAsync(
            p => p.VocabularyTopicId == vocabularyTopicId, cancellationToken);

    public async Task<IReadOnlyList<ReadingPassage>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken)
    {
        var lower = level - levelTolerance;
        var upper = level + levelTolerance;

        var passages = await _db.ReadingPassages
            .Where(p => p.Level >= lower && p.Level <= upper)
            .ToListAsync(cancellationToken);

        // Order by closeness to the learner's level in memory (EF can't translate Abs over the enum cast cleanly).
        return passages
            .OrderBy(p => Math.Abs((int)p.Level - (int)level))
            .ThenByDescending(p => p.CreatedAt)
            .ToList();
    }

    public async Task SaveAsync(ReadingPassage passage, CancellationToken cancellationToken)
    {
        if (_db.Entry(passage).State == EntityState.Detached)
            await _db.ReadingPassages.AddAsync(passage, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReadingPassage>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.ReadingPassages
            .OrderBy(p => p.Level)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var passage = await _db.ReadingPassages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (passage is not null)
        {
            _db.ReadingPassages.Remove(passage);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
