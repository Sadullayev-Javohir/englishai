using Application.Subscription.Ports;
using Domain.Subscription;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Subscription;

/// <summary>
/// Redis-backed <see cref="IUsageCounter"/> (PROJECT-SPEC H.1). Each (learner, feature,
/// period) is an INCR counter with a TTL slightly longer than the longest window, so spent
/// quota is reclaimed automatically after the period rolls over. Wired only when a Redis
/// connection is configured.
/// </summary>
public sealed class RedisUsageCounter : IUsageCounter
{
    // Monthly windows are the longest; keep ~40 days so a counter outlives its period but
    // is still cleaned up well before it could be reused.
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(40);

    private readonly IRedisConnectionProvider _redis;

    public RedisUsageCounter(IRedisConnectionProvider redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.Get(RedisWorkload.Critical).GetDatabase();

    private string Key(Guid learnerId, PremiumFeature feature, string periodKey) =>
        _redis.Key(RedisWorkload.Critical, "usage", $"{learnerId:N}:{(int)feature}:{periodKey}");

    public async Task<int> GetCountAsync(
        Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken)
    {
        var value = await Db.StringGetAsync(Key(learnerId, feature, periodKey));
        return value.HasValue ? (int)value : 0;
    }

    public async Task IncrementAsync(
        Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken)
    {
        var key = Key(learnerId, feature, periodKey);
        await Db.StringIncrementAsync(key);
        await Db.KeyExpireAsync(key, Ttl);
    }
}
