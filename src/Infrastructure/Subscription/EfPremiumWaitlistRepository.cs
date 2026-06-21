using Application.Subscription.Ports;
using Domain.Subscription;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Subscription;

public sealed class EfPremiumWaitlistRepository(EnglishAiDbContext db) : IPremiumWaitlistRepository
{
    public Task<PremiumWaitlistEntry?> GetByContactAsync(string contact, CancellationToken cancellationToken) =>
        db.PremiumWaitlistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Contact == contact, cancellationToken);

    public async Task AddAsync(PremiumWaitlistEntry entry, CancellationToken cancellationToken)
    {
        await db.PremiumWaitlistEntries.AddAsync(entry, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PremiumWaitlistEntry>> GetRecentAsync(
        int limit, CancellationToken cancellationToken) =>
        await db.PremiumWaitlistEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        db.PremiumWaitlistEntries.CountAsync(cancellationToken);
}
