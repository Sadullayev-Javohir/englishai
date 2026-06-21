using System.Text.Json;
using Application.Subscription.Ports;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Speaking;

/// <summary>
/// Redis-backed <see cref="IVoiceLiveSessionStore"/>. Reservations live on the critical workload
/// alongside the cost meter they correct, so a Redis outage takes both out together rather than
/// leaving one half of the accounting running.
///
/// The daily minute counter is delegated to <see cref="IMeteredAllowanceStore"/> so there is exactly
/// one fractional-counter implementation in the app, shared with the daily speaking allowance.
/// </summary>
public sealed class RedisVoiceLiveSessionStore : IVoiceLiveSessionStore
{
    private readonly IRedisConnectionProvider _redis;
    private readonly IMeteredAllowanceStore _allowances;

    public RedisVoiceLiveSessionStore(IRedisConnectionProvider redis, IMeteredAllowanceStore allowances)
    {
        _redis = redis;
        _allowances = allowances;
    }

    private IDatabase Db => _redis.Get(RedisWorkload.Critical).GetDatabase();

    private string SessionKey(string sessionId) =>
        _redis.Key(RedisWorkload.Critical, "voice-live", $"session:{sessionId}");

    public async Task CreateAsync(
        VoiceLiveSessionRecord record,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(record);
        await Db.StringSetAsync(SessionKey(record.SessionId), payload, ttl).WaitAsync(cancellationToken);
    }

    public async Task<VoiceLiveSessionRecord?> TakeAsync(string sessionId, CancellationToken cancellationToken)
    {
        // GETDEL (Redis 6.2+) — read and remove in one round trip so two concurrent completion
        // reports cannot both settle the same reservation.
        var value = await Db.StringGetDeleteAsync(SessionKey(sessionId)).WaitAsync(cancellationToken);
        if (!value.HasValue)
            return null;

        try
        {
            return JsonSerializer.Deserialize<VoiceLiveSessionRecord>(value!);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task<double> AddDailyMinutesAsync(
        string callerKey,
        DateOnly day,
        double deltaMinutes,
        CancellationToken cancellationToken) =>
        _allowances.AddAsync(
            MeteredAllowanceScope.VoiceLiveMinutes, callerKey, PeriodKey(day), deltaMinutes, cancellationToken);

    public Task<double> GetDailyMinutesAsync(
        string callerKey,
        DateOnly day,
        CancellationToken cancellationToken) =>
        _allowances.GetAsync(
            MeteredAllowanceScope.VoiceLiveMinutes, callerKey, PeriodKey(day), cancellationToken);

    // Matches Application.Subscription.UsagePeriodKey.Daily so this counter shares its window with
    // every other daily quota. The caller already resolved the day in Uzbekistan local time.
    private static string PeriodKey(DateOnly day) => $"D:{day:yyyy-MM-dd}";
}
