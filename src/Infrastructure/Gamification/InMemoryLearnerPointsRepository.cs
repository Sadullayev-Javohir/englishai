using System.Collections.Concurrent;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Domain.Gamification;

namespace Infrastructure.Gamification;

/// <summary>
/// In-memory <see cref="ILearnerPointsRepository"/> for dev/tests and for running the app
/// without a database configured. Stores the live aggregate instance keyed by learner id, so
/// in-place mutations are reflected on the next read.
/// </summary>
public sealed class InMemoryLearnerPointsRepository : ILearnerPointsRepository
{
    private readonly ConcurrentDictionary<Guid, LearnerPoints> _byLearner = new();

    /// <summary>Spend receipts, mirroring the unique (learner, action, reference) index in SQL.</summary>
    private readonly ConcurrentDictionary<(Guid Learner, EnergyAction Action, string Reference), DateTimeOffset>
        _spends = new();

    public Task<IReadOnlyList<LearnerPoints>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LearnerPoints>>(_byLearner.Values.ToList());

    public Task<LearnerPoints> GetOrCreateAsync(Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult(_byLearner.GetOrAdd(learnerId, id => LearnerPoints.CreateNew(id, now)));

    public Task SaveAsync(LearnerPoints points, CancellationToken cancellationToken)
    {
        _byLearner[points.LearnerId] = points;
        return Task.CompletedTask;
    }

    public Task<EnergyBalance> ConsumeEnergyAsync(
        Guid learnerId,
        EnergyAction action,
        string referenceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var spendKey = (learnerId, action, referenceId.Trim());
        var points = _byLearner.GetOrAdd(learnerId, id => LearnerPoints.CreateNew(id, now));
        lock (points)
        {
            EnergyOutcome outcome;
            if (_spends.ContainsKey(spendKey))
            {
                points.RefreshEnergy(now);
                outcome = EnergyOutcome.AlreadyStarted;
            }
            else if (points.TryConsumeEnergy(now))
            {
                _spends[spendKey] = now;
                outcome = EnergyOutcome.Consumed;
            }
            else
            {
                outcome = EnergyOutcome.Insufficient;
            }

            return Task.FromResult(new EnergyBalance(
                points.Energy, points.NextEnergyRefillAt, points.FullEnergyRefillAt, outcome));
        }
    }
}
