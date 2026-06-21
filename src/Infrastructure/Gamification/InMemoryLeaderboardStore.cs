using System.Collections.Concurrent;
using Application.Gamification.Ports;
using Domain.Assessment;

namespace Infrastructure.Gamification;

/// <summary>
/// Dev/test <see cref="ILeaderboardStore"/> mirroring the Redis Sorted-Set-per-level layout:
/// one score dictionary per CEFR level. Process-local and resets on restart - fine for
/// dev/tests; <see cref="RedisLeaderboardStore"/> is the durable production path.
/// </summary>
public sealed class InMemoryLeaderboardStore : ILeaderboardStore
{
    private readonly ConcurrentDictionary<CefrLevel, ConcurrentDictionary<Guid, long>> _boards = new();

    public Task ReplaceAllAsync(
        IReadOnlyDictionary<CefrLevel, IReadOnlyDictionary<Guid, long>> scoresByLevel,
        CancellationToken cancellationToken)
    {
        foreach (var level in Enum.GetValues<CefrLevel>())
        {
            var replacement = scoresByLevel.TryGetValue(level, out var scores)
                ? new ConcurrentDictionary<Guid, long>(scores)
                : new ConcurrentDictionary<Guid, long>();
            _boards[level] = replacement;
        }

        return Task.CompletedTask;
    }

    public Task SetScoreAsync(Guid learnerId, CefrLevel level, long lifetimeXp, CancellationToken cancellationToken)
    {
        BoardFor(level)[learnerId] = lifetimeXp;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid learnerId, CefrLevel level, CancellationToken cancellationToken)
    {
        BoardFor(level).TryRemove(learnerId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LeaderboardRankEntry>> GetTopAsync(
        CefrLevel level, int count, CancellationToken cancellationToken)
    {
        IReadOnlyList<LeaderboardRankEntry> top = OrderedEntries(level)
            .Take(count)
            .Select((kv, index) => new LeaderboardRankEntry(kv.Key, kv.Value, Rank: index + 1))
            .ToList();

        return Task.FromResult(top);
    }

    public Task<LeaderboardRankEntry?> GetLearnerRankAsync(
        Guid learnerId, CefrLevel level, CancellationToken cancellationToken)
    {
        var ordered = OrderedEntries(level).ToList();
        var index = ordered.FindIndex(kv => kv.Key == learnerId);

        LeaderboardRankEntry? entry = index < 0
            ? null
            : new LeaderboardRankEntry(learnerId, ordered[index].Value, Rank: index + 1);

        return Task.FromResult(entry);
    }

    private ConcurrentDictionary<Guid, long> BoardFor(CefrLevel level) =>
        _boards.GetOrAdd(level, _ => new ConcurrentDictionary<Guid, long>());

    // Ties break by learner id for a deterministic order - the exact tie-break rule doesn't
    // need to match Redis's, since this store never runs alongside the Redis one.
    private IEnumerable<KeyValuePair<Guid, long>> OrderedEntries(CefrLevel level) =>
        BoardFor(level).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key);
}
