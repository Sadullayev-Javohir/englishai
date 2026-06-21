using Application.Competition.Ports;
using Domain.Competition;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Competition;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="ICompetitionRepository"/>. Loads the full
/// aggregate (slides, participants and their answers) so the real-time game logic runs
/// in memory on the tracked entity.
/// </summary>
public sealed class EfCompetitionRepository : ICompetitionRepository
{
    private readonly EnglishAiDbContext _db;

    public EfCompetitionRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<Domain.Competition.Competition?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Competitions
            .Include(c => c.Slides)
            .Include(c => c.Participants)
            .ThenInclude(p => p.Answers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Domain.Competition.Competition competition, CancellationToken cancellationToken)
    {
        await _db.Competitions.AddAsync(competition, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Domain.Competition.Competition competition, CancellationToken cancellationToken)
    {
        if (_db.Entry(competition).State == EntityState.Detached)
            _db.Competitions.Update(competition);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Competition.Competition>> ListByHostAsync(
        Guid hostLearnerId, CancellationToken cancellationToken) =>
        await _db.Competitions
            .Where(c => c.HostLearnerId == hostLearnerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Domain.Competition.Competition>> ListActiveAsync(CancellationToken cancellationToken) =>
        await _db.Competitions
            .Where(c => c.Status == CompetitionStatus.Active)
            .ToListAsync(cancellationToken);
}
