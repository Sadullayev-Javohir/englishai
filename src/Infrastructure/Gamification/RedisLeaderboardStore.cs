using Application.Gamification.Ports;
using Domain.Assessment;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Gamification;

/// <summary>
/// Redis-backed <see cref="ILeaderboardStore"/>: one Sorted Set per CEFR level (member =
/// learner id, score = lifetime XP), the natural extension of the Sorted Set
/// <see cref="RedisGamificationStore"/> already uses for streaks. <c>ZREVRANGE</c>/
/// <c>ZREVRANK</c> give O(log N) ranked reads without a separate ranking pass.
/// </summary>
public sealed class RedisLeaderboardStore : ILeaderboardStore
{
    private readonly IRedisConnectionProvider _redis;

    public RedisLeaderboardStore(IRedisConnectionProvider redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.Get(RedisWorkload.Cache).GetDatabase();

    private string LevelKey(CefrLevel level) => _redis.Key(RedisWorkload.Cache, "leaderboard", level.ToString());
    private static string Member(Guid learnerId) => learnerId.ToString("N");
    private static Guid ParseMember(RedisValue member) => Guid.ParseExact(member.ToString(), "N");

    public async Task ReplaceAllAsync(
        IReadOnlyDictionary<CefrLevel, IReadOnlyDictionary<Guid, long>> scoresByLevel,
        CancellationToken cancellationToken)
    {
        foreach (var level in Enum.GetValues<CefrLevel>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = LevelKey(level);
            var temporaryKey = $"{key}:rebuild:{Guid.NewGuid():N}";

            if (scoresByLevel.TryGetValue(level, out var scores) && scores.Count > 0)
            {
                var entries = scores
                    .Select(score => new SortedSetEntry(Member(score.Key), score.Value))
                    .ToArray();
                await Db.SortedSetAddAsync(temporaryKey, entries);
                await Db.KeyRenameAsync(temporaryKey, key, When.Always);
            }
            else
            {
                await Db.KeyDeleteAsync(key);
            }
        }
    }

    public Task SetScoreAsync(Guid learnerId, CefrLevel level, long lifetimeXp, CancellationToken cancellationToken) =>
        Db.SortedSetAddAsync(LevelKey(level), Member(learnerId), lifetimeXp);

    public Task RemoveAsync(Guid learnerId, CefrLevel level, CancellationToken cancellationToken) =>
        Db.SortedSetRemoveAsync(LevelKey(level), Member(learnerId));

    public async Task<IReadOnlyList<LeaderboardRankEntry>> GetTopAsync(
        CefrLevel level, int count, CancellationToken cancellationToken)
    {
        var results = await Db.SortedSetRangeByRankWithScoresAsync(
            LevelKey(level), start: 0, stop: count - 1, order: Order.Descending);

        var entries = new List<LeaderboardRankEntry>(results.Length);
        for (var i = 0; i < results.Length; i++)
            entries.Add(new LeaderboardRankEntry(ParseMember(results[i].Element), (long)results[i].Score, Rank: i + 1));

        return entries;
    }

    public async Task<LeaderboardRankEntry?> GetLearnerRankAsync(
        Guid learnerId, CefrLevel level, CancellationToken cancellationToken)
    {
        var key = LevelKey(level);
        var member = Member(learnerId);

        var score = await Db.SortedSetScoreAsync(key, member);
        if (score is null)
            return null;

        var rank = await Db.SortedSetRankAsync(key, member, Order.Descending);
        if (rank is null)
            return null;

        return new LeaderboardRankEntry(learnerId, (long)score.Value, Rank: (int)rank.Value + 1);
    }
}
