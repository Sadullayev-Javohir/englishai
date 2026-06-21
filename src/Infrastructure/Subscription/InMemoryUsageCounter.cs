using System.Collections.Concurrent;
using Application.Subscription.Ports;
using Domain.Subscription;

namespace Infrastructure.Subscription;

/// <summary>
/// Dev/test <see cref="IUsageCounter"/>. Keys by learner+feature+period so daily/monthly
/// windows are independent. Process-local; the Redis adapter is the durable production path.
/// </summary>
public sealed class InMemoryUsageCounter : IUsageCounter
{
    private readonly ConcurrentDictionary<string, int> _counts = new();

    private static string Key(Guid learnerId, PremiumFeature feature, string periodKey) =>
        $"{learnerId:N}:{(int)feature}:{periodKey}";

    public Task<int> GetCountAsync(
        Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken)
    {
        _counts.TryGetValue(Key(learnerId, feature, periodKey), out var count);
        return Task.FromResult(count);
    }

    public Task IncrementAsync(
        Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken)
    {
        _counts.AddOrUpdate(Key(learnerId, feature, periodKey), 1, (_, current) => current + 1);
        return Task.CompletedTask;
    }
}
