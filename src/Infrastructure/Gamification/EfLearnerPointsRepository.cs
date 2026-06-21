using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Domain.Gamification;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Gamification;

/// <summary>EF Core / PostgreSQL adapter for <see cref="ILearnerPointsRepository"/>.</summary>
public sealed class EfLearnerPointsRepository : ILearnerPointsRepository
{
    private readonly EnglishAiDbContext _db;

    public EfLearnerPointsRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LearnerPoints>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.LearnerPoints.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<LearnerPoints> GetOrCreateAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _db.LearnerPoints.FirstOrDefaultAsync(p => p.LearnerId == learnerId, cancellationToken);
        return existing ?? LearnerPoints.CreateNew(learnerId, now);
    }

    public async Task SaveAsync(LearnerPoints points, CancellationToken cancellationToken)
    {
        if (_db.Entry(points).State == EntityState.Detached)
            await _db.LearnerPoints.AddAsync(points, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EnergyBalance> ConsumeEnergyAsync(
        Guid learnerId,
        EnergyAction action,
        string referenceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var reference = referenceId.Trim();
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            // Serialize this learner's energy for the whole transaction so two parallel opens
            // can neither double-spend nor both miss the spend receipt below.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({learnerId.ToString()}, 0))",
                cancellationToken);

            var points = await _db.LearnerPoints
                .FirstOrDefaultAsync(p => p.LearnerId == learnerId, cancellationToken)
                ?? LearnerPoints.CreateNew(learnerId, now);

            var alreadyPaid = await _db.EnergySpends.AnyAsync(
                s => s.LearnerId == learnerId && s.Action == action && s.ReferenceId == reference,
                cancellationToken);

            EnergyOutcome outcome;
            if (alreadyPaid)
            {
                // Nothing to debit, but the caller still gets an up-to-date bar and timers.
                points.RefreshEnergy(now);
                outcome = EnergyOutcome.AlreadyStarted;
            }
            else
            {
                outcome = points.TryConsumeEnergy(now)
                    ? EnergyOutcome.Consumed
                    : EnergyOutcome.Insufficient;
            }

            if (outcome == EnergyOutcome.Consumed)
                await _db.EnergySpends.AddAsync(
                    EnergySpend.Record(learnerId, action, reference, now), cancellationToken);

            // An already-paid or rejected attempt still refreshed the bar, so persist either way.
            if (_db.Entry(points).State == EntityState.Detached)
                await _db.LearnerPoints.AddAsync(points, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new EnergyBalance(
                points.Energy, points.NextEnergyRefillAt, points.FullEnergyRefillAt, outcome);
        });
    }
}
