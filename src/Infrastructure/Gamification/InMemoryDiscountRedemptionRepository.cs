using System.Collections.Concurrent;
using Application.Gamification.Ports;
using Domain.Gamification;

namespace Infrastructure.Gamification;

/// <summary>
/// In-memory <see cref="IDiscountRedemptionRepository"/> for dev/tests and for running the app
/// without a database configured. Keyed by coupon id, with a secondary lookup by code.
/// </summary>
public sealed class InMemoryDiscountRedemptionRepository : IDiscountRedemptionRepository
{
    private readonly ConcurrentDictionary<Guid, DiscountRedemption> _byId = new();

    public Task<DiscountRedemption?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.FirstOrDefault(r => r.Code == code));

    public Task<IReadOnlyList<DiscountRedemption>> GetActiveForLearnerAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscountRedemption> active = _byId.Values
            .Where(r => r.LearnerId == learnerId && r.Status == DiscountRedemptionStatus.Active && r.ExpiresAt >= now)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(active);
    }

    public Task SaveAsync(DiscountRedemption redemption, CancellationToken cancellationToken)
    {
        _byId[redemption.Id] = redemption;
        return Task.CompletedTask;
    }
}
