using Domain.Gamification;

namespace Application.Gamification.Ports;

/// <summary>
/// Durable persistence port for <see cref="DiscountRedemption"/> coupons (leaderboard/points
/// feature). EF Core adapter (PostgreSQL) in production, in-memory for dev/tests.
/// </summary>
public interface IDiscountRedemptionRepository
{
    /// <summary>Finds a coupon by its unique code, or null if no such code was ever issued.</summary>
    Task<DiscountRedemption?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>The learner's still-active (unused, unexpired) coupons, newest first.</summary>
    Task<IReadOnlyList<DiscountRedemption>> GetActiveForLearnerAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Inserts a new coupon or persists changes (e.g. marking one used) to an existing one.</summary>
    Task SaveAsync(DiscountRedemption redemption, CancellationToken cancellationToken);
}
