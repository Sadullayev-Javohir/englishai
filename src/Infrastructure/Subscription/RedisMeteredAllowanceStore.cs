using System.Globalization;
using Application.Subscription.Ports;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Subscription;

/// <summary>
/// Redis-backed <see cref="IMeteredAllowanceStore"/>, using INCRBYFLOAT so a fractional amount is
/// added atomically under concurrent turns. Lives on the critical workload: an evicted allowance
/// counter hands out free capacity, which is a billing decision, not a cache miss.
/// </summary>
public sealed class RedisMeteredAllowanceStore(IRedisConnectionProvider redis) : IMeteredAllowanceStore
{
    // A period counter only needs to outlive its own window; two days absorbs clock skew and the
    // UTC+5 offset without keeping stale counters around.
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(2);

    private IDatabase Db => redis.Get(RedisWorkload.Critical).GetDatabase();

    public async Task<double> AddAsync(
        string scope,
        string subjectKey,
        string periodKey,
        double delta,
        CancellationToken cancellationToken = default)
    {
        var key = Key(scope, subjectKey, periodKey);
        var total = await Db.StringIncrementAsync(key, delta).WaitAsync(cancellationToken);
        await Db.KeyExpireAsync(key, Ttl).WaitAsync(cancellationToken);

        if (total < 0)
        {
            // Reconciling a reservation should never drive the counter below zero, but rounding or a
            // lost reservation must not turn into unlimited free allowance.
            await Db.StringSetAsync(key, 0d.ToString("R", CultureInfo.InvariantCulture), Ttl)
                .WaitAsync(cancellationToken);
            return 0;
        }

        return total;
    }

    public async Task<double> GetAsync(
        string scope,
        string subjectKey,
        string periodKey,
        CancellationToken cancellationToken = default)
    {
        var value = await Db.StringGetAsync(Key(scope, subjectKey, periodKey)).WaitAsync(cancellationToken);
        if (!value.HasValue)
            return 0;

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
            ? Math.Max(0, amount)
            : 0;
    }

    private string Key(string scope, string subjectKey, string periodKey) =>
        redis.Key(RedisWorkload.Critical, "allowance", $"{scope}:{periodKey}:{subjectKey}");
}
