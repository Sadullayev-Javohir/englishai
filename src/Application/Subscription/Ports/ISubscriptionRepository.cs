using Domain.Subscription;

namespace Application.Subscription.Ports;

/// <summary>
/// Persistence port for the subscription aggregate (one per learner).
/// EF Core adapter in production, in-memory for dev/tests.
/// </summary>
public interface ISubscriptionRepository
{
    Task<Domain.Subscription.Subscription?> GetByLearnerIdAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Subscriptions for every learner in <paramref name="learnerIds"/> that has one, keyed by
    /// learner id. Used to bulk-resolve Premium status for a page of results (e.g. the
    /// leaderboard) without one round-trip per row.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Domain.Subscription.Subscription>> GetManyByLearnerIdsAsync(
        IReadOnlyCollection<Guid> learnerIds, CancellationToken cancellationToken);

    /// <summary>
    /// All subscriptions in a paid state (Premium or Cancelled), for the daily expiry job
    /// to scan for renewal reminders and lapsed periods.
    /// </summary>
    Task<IReadOnlyList<Domain.Subscription.Subscription>> GetPaidAsync(CancellationToken cancellationToken);

    Task SaveAsync(Domain.Subscription.Subscription subscription, CancellationToken cancellationToken);
}
