using Application.Listening.Ports;
using Domain.Assessment;
using Domain.Listening;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Listening;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IListeningRepository"/>. The owned question
/// collection loads and persists with each exercise.
/// </summary>
public sealed class EfListeningRepository : IListeningRepository
{
    private readonly EnglishAiDbContext _db;

    public EfListeningRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<ListeningExercise?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.ListeningExercises.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<ListeningExercise?> GetByTopicIdAsync(
        Guid vocabularyTopicId, CancellationToken cancellationToken) =>
        await _db.ListeningExercises
            .FirstOrDefaultAsync(e => e.VocabularyTopicId == vocabularyTopicId, cancellationToken);

    public async Task<IReadOnlyList<ListeningExercise>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken)
    {
        var lower = level - levelTolerance;
        var upper = level + levelTolerance;

        var exercises = await _db.ListeningExercises
            .Where(e => e.Level >= lower && e.Level <= upper)
            .ToListAsync(cancellationToken);

        // Order by closeness to the learner's level in memory (EF can't translate Abs over the enum cast cleanly).
        return exercises
            .OrderBy(e => Math.Abs((int)e.Level - (int)level))
            .ThenByDescending(e => e.CreatedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<ListeningExercise>> GetAllOrderedByLevelAsync(CancellationToken cancellationToken) =>
        await _db.ListeningExercises
            .OrderBy(e => e.Level)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(ListeningExercise exercise, CancellationToken cancellationToken)
    {
        if (_db.Entry(exercise).State == EntityState.Detached)
            await _db.ListeningExercises.AddAsync(exercise, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var exercise = await _db.ListeningExercises.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (exercise is not null)
        {
            _db.ListeningExercises.Remove(exercise);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
