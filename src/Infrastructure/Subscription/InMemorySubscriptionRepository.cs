using System.Collections.Concurrent;
using Application.Subscription.Ports;
using Domain.Subscription;

namespace Infrastructure.Subscription;

/// <summary>
/// Dev/test <see cref="ISubscriptionRepository"/> (one subscription per learner). Used when
/// no database is configured; EF Core is the durable production path.
/// </summary>
public sealed class InMemorySubscriptionRepository : ISubscriptionRepository
{
    private readonly ConcurrentDictionary<Guid, Domain.Subscription.Subscription> _byLearner = new();

    public Task<Domain.Subscription.Subscription?> GetByLearnerIdAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        _byLearner.TryGetValue(learnerId, out var subscription);
        return Task.FromResult(subscription);
    }

    public Task<IReadOnlyDictionary<Guid, Domain.Subscription.Subscription>> GetManyByLearnerIdsAsync(
        IReadOnlyCollection<Guid> learnerIds, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, Domain.Subscription.Subscription> result = _byLearner
            .Where(kv => learnerIds.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Domain.Subscription.Subscription>> GetPaidAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Subscription.Subscription> paid = _byLearner.Values
            .Where(s => s.Status is SubscriptionStatus.Premium or SubscriptionStatus.Cancelled)
            .ToList();
        return Task.FromResult(paid);
    }

    public Task SaveAsync(Domain.Subscription.Subscription subscription, CancellationToken cancellationToken)
    {
        _byLearner[subscription.LearnerId] = subscription;
        return Task.CompletedTask;
    }
}
