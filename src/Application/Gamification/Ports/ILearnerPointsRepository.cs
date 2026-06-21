using Application.Gamification.Dtos;
using Domain.Gamification;

namespace Application.Gamification.Ports;

public sealed record EnergyBalance(
    int Current,
    DateTimeOffset? NextRefillAt,
    DateTimeOffset? FullRefillAt,
    EnergyOutcome Outcome);

/// <summary>
/// Durable persistence port for the <see cref="LearnerPoints"/> ledger (leaderboard/points
/// feature). EF Core adapter (PostgreSQL) in production, in-memory for dev/tests - unlike the
/// Redis-only streak counters, this balance must never be lost.
/// </summary>
public interface ILearnerPointsRepository
{
    /// <summary>Returns every durable ledger for rebuilding derived leaderboard indexes.</summary>
    Task<IReadOnlyList<LearnerPoints>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Returns the learner's ledger, creating a fresh zero-balance one if none exists yet.</summary>
    Task<LearnerPoints> GetOrCreateAsync(Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Inserts a new ledger or updates the existing one for its learner.</summary>
    Task SaveAsync(LearnerPoints points, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically refills what is due and spends one unit on starting <paramref name="action"/>
    /// for <paramref name="referenceId"/>. Spending is idempotent per
    /// (learner, action, reference): a repeat attempt returns
    /// <see cref="EnergyOutcome.AlreadyStarted"/> without debiting again, and an empty bar
    /// returns <see cref="EnergyOutcome.Insufficient"/> without changing anything.
    /// </summary>
    Task<EnergyBalance> ConsumeEnergyAsync(
        Guid learnerId,
        EnergyAction action,
        string referenceId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
