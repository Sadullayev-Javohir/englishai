using Application.Gamification.Ports;
using Application.Learning.Ports;
using Domain.Assessment;

namespace Application.Gamification;

public sealed record LeaderboardRebuildResult(int LedgerCount, int IndexedCount);

public sealed class RebuildLeaderboardService
{
    private readonly ILearnerPointsRepository _points;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ILeaderboardStore _leaderboard;

    public RebuildLeaderboardService(
        ILearnerPointsRepository points,
        ILearnerProfileRepository profiles,
        ILeaderboardStore leaderboard)
    {
        _points = points;
        _profiles = profiles;
        _leaderboard = leaderboard;
    }

    public async Task<LeaderboardRebuildResult> RebuildAsync(CancellationToken cancellationToken)
    {
        var ledgers = await _points.GetAllAsync(cancellationToken);
        var profiles = (await _profiles.GetAllAsync(cancellationToken))
            .ToDictionary(profile => profile.LearnerId, profile => profile.OverallLevel);

        var scoresByLevel = Enum.GetValues<CefrLevel>()
            .ToDictionary<CefrLevel, CefrLevel, IReadOnlyDictionary<Guid, long>>(
                level => level,
                _ => new Dictionary<Guid, long>());

        var mutableScores = scoresByLevel.ToDictionary(
            entry => entry.Key,
            entry => (Dictionary<Guid, long>)entry.Value);

        foreach (var ledger in ledgers.Where(ledger => ledger.LifetimeXp > 0))
        {
            var level = profiles.GetValueOrDefault(ledger.LearnerId, CefrLevel.A2);
            mutableScores[level][ledger.LearnerId] = ledger.LifetimeXp;
        }

        await _leaderboard.ReplaceAllAsync(scoresByLevel, cancellationToken);
        return new LeaderboardRebuildResult(ledgers.Count, mutableScores.Sum(entry => entry.Value.Count));
    }
}
