using System.Text.Json;
using Application.Speaking.Ports;
using Domain.Speaking;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Speaking;

public sealed class RedisConversationStore(IRedisConnectionProvider redis) : IConversationStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(6);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ConversationSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var value = await redis.Get(RedisWorkload.Critical).GetDatabase().StringGetAsync(Key(sessionId)).WaitAsync(cancellationToken);
        if (value.IsNullOrEmpty) return null;
        var snapshot = JsonSerializer.Deserialize<ConversationSessionSnapshot>((string)value!, JsonOptions);
        return snapshot is null ? null : ConversationSession.Restore(snapshot);
    }

    public Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default) =>
        redis.Get(RedisWorkload.Critical).GetDatabase().StringSetAsync(Key(session.Id), JsonSerializer.Serialize(session.ToSnapshot(), JsonOptions), Lifetime)
            .WaitAsync(cancellationToken);

    private string Key(Guid id) => redis.Key(RedisWorkload.Critical, "speaking", id.ToString("N"));
}
