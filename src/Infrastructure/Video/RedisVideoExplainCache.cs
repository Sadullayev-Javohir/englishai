using Application.Video.Ports;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Video;

public sealed class RedisVideoExplainCache(IRedisConnectionProvider redis) : IVideoExplainCache
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromHours(24);
    private readonly IDatabase _database = redis.Get(RedisWorkload.Cache).GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(redis.Key(RedisWorkload.Cache, "ai", $"video-explain:{key}"));
        return value.HasValue ? value.ToString() : null;
    }

    public Task SetAsync(string key, string reply, CancellationToken cancellationToken = default) =>
        _database.StringSetAsync(redis.Key(RedisWorkload.Cache, "ai", $"video-explain:{key}"), reply, TimeToLive);
}
