using Application.Grammar.Ports;
using Domain.Assessment;
using Domain.Grammar;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Grammar;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IGrammarRepository"/>. The owned exercise
/// and application-task collections load and persist with each lesson.
/// </summary>
public sealed class EfGrammarRepository : IGrammarRepository
{
    private readonly EnglishAiDbContext _db;

    public EfGrammarRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<GrammarLesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.GrammarLessons
            .Include(l => l.Examples)
            .Include(l => l.CommonMistakes)
            .Include(l => l.Exercises)
            .Include(l => l.ApplicationTasks)
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GrammarLesson>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await _db.GrammarLessons
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken);

    public async Task<GrammarLesson?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken) =>
        await _db.GrammarLessons.FirstOrDefaultAsync(
            l => l.VocabularyTopicId == vocabularyTopicId, cancellationToken);

    public async Task<IReadOnlyList<GrammarLesson>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken)
    {
        var lower = level - levelTolerance;
        var upper = level + levelTolerance;

        var lessons = await _db.GrammarLessons
            .Where(l => l.Level >= lower && l.Level <= upper)
            .ToListAsync(cancellationToken);

        // Order by Uzbek-learner difficulty priority first (lower category value = higher
        // priority), then by closeness to the learner's level (G.2). Done in memory because
        // EF can't translate Abs over the enum cast cleanly.
        return lessons
            .OrderBy(l => (int)l.Category)
            .ThenBy(l => Math.Abs((int)l.Level - (int)level))
            .ThenByDescending(l => l.CreatedAt)
            .ToList();
    }

    public async Task SaveAsync(GrammarLesson lesson, CancellationToken cancellationToken)
    {
        if (_db.Entry(lesson).State == EntityState.Detached)
            await _db.GrammarLessons.AddAsync(lesson, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrammarLesson>> GetAllAsync(CancellationToken cancellationToken)
    {
        var lessons = await _db.GrammarLessons
            .OrderBy(l => l.Level)
            .ThenByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
        return lessons;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _db.GrammarLessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lesson is not null)
        {
            _db.GrammarLessons.Remove(lesson);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
