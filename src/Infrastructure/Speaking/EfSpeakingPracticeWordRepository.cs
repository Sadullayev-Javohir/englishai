using Application.Speaking.Ports;
using Domain.Speaking;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Speaking;

public sealed class EfSpeakingPracticeWordRepository : ISpeakingPracticeWordRepository
{
    private readonly EnglishAiDbContext _db;

    public EfSpeakingPracticeWordRepository(EnglishAiDbContext db) => _db = db;

    public Task<SpeakingPracticeWord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SpeakingPracticeWords.FirstOrDefaultAsync(word => word.Id == id, cancellationToken);

    public Task<SpeakingPracticeWord?> GetByLearnerAndWordAsync(
        Guid learnerId, string normalizedWord, CancellationToken cancellationToken = default) =>
        _db.SpeakingPracticeWords.FirstOrDefaultAsync(
            word => word.LearnerId == learnerId && word.NormalizedWord == normalizedWord,
            cancellationToken);

    public async Task<IReadOnlyList<SpeakingPracticeWord>> GetActiveAsync(
        Guid learnerId, CancellationToken cancellationToken = default) =>
        await _db.SpeakingPracticeWords
            .AsNoTracking()
            .Where(word => word.LearnerId == learnerId && word.MasteredAt == null)
            .OrderByDescending(word => word.LastFailedAt)
            .ThenBy(word => word.Word)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(SpeakingPracticeWord word, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(word).State == EntityState.Detached)
            await _db.SpeakingPracticeWords.AddAsync(word, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
