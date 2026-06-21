using Application.Gamification.Ports;
using Domain.Learning;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Gamification;

/// <summary>
/// Redis-backed <see cref="IGamificationStore"/> (PROJECT-SPEC Faza 5). Today's task count
/// is a short-lived INCR counter; completed days are held in a per-learner Sorted Set
/// (member and score are both the day number), which is the spec's "Redis Sorted Sets"
/// substrate for streaks and future leaderboards. Wired only when a Redis connection is
/// configured; otherwise the in-memory adapter is used.
/// </summary>
public sealed class RedisGamificationStore : IGamificationStore
{
    // Daily counters expire automatically - only "today" is ever read, so keep two days
    // of headroom around the day boundary and let Redis reclaim the rest.
    private static readonly TimeSpan CounterTtl = TimeSpan.FromDays(2);

    private readonly IRedisConnectionProvider _redis;

    public RedisGamificationStore(IRedisConnectionProvider redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.Get(RedisWorkload.Critical).GetDatabase();

    private string CountKey(Guid learnerId, DateOnly day) =>
        _redis.Key(RedisWorkload.Critical, "gamification", $"count:{learnerId:N}:{day:yyyy-MM-dd}");

    private string StreakKey(Guid learnerId) =>
        _redis.Key(RedisWorkload.Critical, "gamification", $"streak:{learnerId:N}");

    private string SkillsKey(Guid learnerId, DateOnly day) =>
        _redis.Key(RedisWorkload.Critical, "gamification", $"skills:{learnerId:N}:{day:yyyy-MM-dd}");

    public async Task<int> IncrementTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
    {
        var key = CountKey(learnerId, day);
        var count = await Db.StringIncrementAsync(key);
        await Db.KeyExpireAsync(key, CounterTtl);
        return (int)count;
    }

    public async Task<int> GetTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
    {
        var value = await Db.StringGetAsync(CountKey(learnerId, day));
        return value.HasValue ? (int)value : 0;
    }

    public async Task<IReadOnlyCollection<SkillType>> MarkSkillPracticedAsync(
        Guid learnerId, DateOnly day, SkillType skill, CancellationToken cancellationToken)
    {
        // A per-day Set keyed by skill: SADD is idempotent, so the same skill twice is a no-op.
        var key = SkillsKey(learnerId, day);
        await Db.SetAddAsync(key, ((int)skill).ToString());
        await Db.KeyExpireAsync(key, CounterTtl);
        return await ReadSkillsAsync(key);
    }

    public async Task<IReadOnlyCollection<SkillType>> GetSkillsPracticedTodayAsync(
        Guid learnerId, DateOnly day, CancellationToken cancellationToken) =>
        await ReadSkillsAsync(SkillsKey(learnerId, day));

    private async Task<IReadOnlyCollection<SkillType>> ReadSkillsAsync(string key)
    {
        var members = await Db.SetMembersAsync(key);
        var skills = new List<SkillType>(members.Length);
        foreach (var member in members)
        {
            if (int.TryParse(member, out var value) && Enum.IsDefined(typeof(SkillType), value))
                skills.Add((SkillType)value);
        }

        return skills;
    }

    public async Task MarkDayCompletedAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
    {
        // Sorted Set member = score = day number, so reads come back ordered and adding the
        // same day again is a no-op (idempotent).
        await Db.SortedSetAddAsync(StreakKey(learnerId), day.DayNumber.ToString(), day.DayNumber);
    }

    public async Task<IReadOnlyCollection<DateOnly>> GetCompletedDaysAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        var members = await Db.SortedSetRangeByScoreAsync(StreakKey(learnerId));
        var days = new List<DateOnly>(members.Length);
        foreach (var member in members)
        {
            if (int.TryParse(member, out var dayNumber))
                days.Add(DateOnly.FromDayNumber(dayNumber));
        }

        return days;
    }

    public async Task DeleteLearnerAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        // The streak Sorted Set is the only durable, unbounded key - delete it outright. The
        // per-day counters/skills carry a short TTL and only ever cover days near "today", so
        // clear the current window explicitly and let any older ones expire on their own.
        var keys = new List<RedisKey> { StreakKey(learnerId) };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var offset = -1; offset <= 1; offset++)
        {
            var day = today.AddDays(offset);
            keys.Add(CountKey(learnerId, day));
            keys.Add(SkillsKey(learnerId, day));
        }

        await Db.KeyDeleteAsync(keys.ToArray());
    }
}
