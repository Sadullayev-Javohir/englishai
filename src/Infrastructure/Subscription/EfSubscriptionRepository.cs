using Application.Subscription.Ports;
using Domain.Subscription;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Subscription;

/// <summary>EF Core (PostgreSQL) <see cref="ISubscriptionRepository"/>.</summary>
public sealed class EfSubscriptionRepository : ISubscriptionRepository
{
    private readonly EnglishAiDbContext _db;

    public EfSubscriptionRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public Task<Domain.Subscription.Subscription?> GetByLearnerIdAsync(
        Guid learnerId, CancellationToken cancellationToken) =>
        _db.Subscriptions.FirstOrDefaultAsync(s => s.LearnerId == learnerId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Domain.Subscription.Subscription>> GetManyByLearnerIdsAsync(
        IReadOnlyCollection<Guid> learnerIds, CancellationToken cancellationToken)
    {
        var subscriptions = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => learnerIds.Contains(s.LearnerId))
            .ToListAsync(cancellationToken);

        return subscriptions.ToDictionary(s => s.LearnerId);
    }

    public async Task<IReadOnlyList<Domain.Subscription.Subscription>> GetPaidAsync(
        CancellationToken cancellationToken) =>
        await _db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Premium || s.Status == SubscriptionStatus.Cancelled)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(Domain.Subscription.Subscription subscription, CancellationToken cancellationToken)
    {
        var exists = await _db.Subscriptions.AnyAsync(s => s.Id == subscription.Id, cancellationToken);
        if (exists)
            _db.Subscriptions.Update(subscription);
        else
            await _db.Subscriptions.AddAsync(subscription, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
