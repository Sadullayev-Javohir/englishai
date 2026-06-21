using Application.Gamification.Ports;
using Domain.Gamification;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Gamification;

/// <summary>EF Core / PostgreSQL adapter for <see cref="IDiscountRedemptionRepository"/>.</summary>
public sealed class EfDiscountRedemptionRepository : IDiscountRedemptionRepository
{
    private readonly EnglishAiDbContext _db;

    public EfDiscountRedemptionRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public Task<DiscountRedemption?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _db.DiscountRedemptions.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<DiscountRedemption>> GetActiveForLearnerAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await _db.DiscountRedemptions
            .Where(r => r.LearnerId == learnerId
                && r.Status == DiscountRedemptionStatus.Active
                && r.ExpiresAt >= now)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(DiscountRedemption redemption, CancellationToken cancellationToken)
    {
        if (_db.Entry(redemption).State == EntityState.Detached)
            await _db.DiscountRedemptions.AddAsync(redemption, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
