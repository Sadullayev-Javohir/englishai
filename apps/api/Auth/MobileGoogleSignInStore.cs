using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Text.Json;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Web.Auth;

public sealed class MobileGoogleSignInStore(IRedisConnectionProvider? redis)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private const string RedeemScript = """
        local value = redis.call('GET', KEYS[1])
        if not value then return nil end
        local parsed = cjson.decode(value)
        if parsed.State ~= ARGV[1] then return nil end
        redis.call('DEL', KEYS[1])
        return parsed.IdToken
        """;

    public async Task<string> PutAsync(string state, string idToken, CancellationToken cancellationToken)
    {
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        if (redis is null)
        {
            _entries[code] = new Entry(state, idToken);
            return code;
        }

        var payload = JsonSerializer.Serialize(new Entry(state, idToken));
        await redis.Get(RedisWorkload.Critical).GetDatabase().StringSetAsync(Key(code), payload, Lifetime).WaitAsync(cancellationToken);
        return code;
    }

    public async Task<string?> RedeemAsync(string code, string state, CancellationToken cancellationToken)
    {
        if (redis is null)
        {
            if (!_entries.TryRemove(code, out var entry) || entry.State != state)
                return null;
            return entry.IdToken;
        }

        var result = await redis.Get(RedisWorkload.Critical).GetDatabase().ScriptEvaluateAsync(RedeemScript, [Key(code)], [state]).WaitAsync(cancellationToken);
        return result.IsNull ? null : (string?)result;
    }

    private string Key(string code) => redis!.Key(RedisWorkload.Critical, "auth-exchange", code);
    private sealed record Entry(string State, string IdToken);
}
