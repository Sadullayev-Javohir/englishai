using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.RateLimiting;

public sealed record RateLimitLease(bool Allowed, int RetryAfterSeconds, long Count, bool UsedFallback = false);

public interface IDistributedRateLimitStore
{
    Task<RateLimitLease> AcquireAsync(string key, int permitLimit, TimeSpan window, CancellationToken cancellationToken);
}

public sealed class RedisDistributedRateLimitStore(IRedisConnectionProvider redis) : IDistributedRateLimitStore
{
    private const string Script = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
          redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        local ttl = redis.call('PTTL', KEYS[1])
        return { current, ttl }
        """;

    public async Task<RateLimitLease> AcquireAsync(string key, int permitLimit, TimeSpan window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var evaluated = await redis.Get(RedisWorkload.Cache).GetDatabase().ScriptEvaluateAsync(
            Script,
            [redis.Key(RedisWorkload.Cache, "ratelimit", key)],
            [(long)Math.Max(1, window.TotalMilliseconds)]).WaitAsync(cancellationToken);
        var result = (RedisResult[]?)evaluated ?? throw new RedisServerException("Rate limit script returned no result.");
        var count = (long)result[0];
        var ttlMilliseconds = Math.Max(1, (long)result[1]);
        return new RateLimitLease(
            count <= Math.Max(1, permitLimit),
            Math.Max(1, (int)Math.Ceiling(ttlMilliseconds / 1000d)),
            count);
    }
}

public sealed class UnavailableDistributedRateLimitStore : IDistributedRateLimitStore
{
    public Task<RateLimitLease> AcquireAsync(string key, int permitLimit, TimeSpan window, CancellationToken cancellationToken) =>
        throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is not configured.");
}
