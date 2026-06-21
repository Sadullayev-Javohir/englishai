using Domain.Subscription;

namespace Application.Subscription.Ports;

/// <summary>
/// Tracks how many times a learner has used a metered feature within a period
/// (PROJECT-SPEC H.1). The <paramref name="periodKey"/> identifies the window (e.g. a day
/// or a month) and is computed by the entitlement service. Backed by Redis (INCR with TTL)
/// in production and an in-memory adapter in dev/tests.
/// </summary>
public interface IUsageCounter
{
    Task<int> GetCountAsync(Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken);

    Task IncrementAsync(Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken);
}
