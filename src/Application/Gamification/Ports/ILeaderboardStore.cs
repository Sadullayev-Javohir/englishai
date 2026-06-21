using Domain.Assessment;

namespace Application.Gamification.Ports;

/// <summary>A single leaderboard row: a learner id ranked by their lifetime XP score.</summary>
/// <param name="LearnerId">The learner this row belongs to.</param>
/// <param name="Score">Their <see cref="Domain.Gamification.LearnerPoints.LifetimeXp"/>.</param>
/// <param name="Rank">1-based rank within the level's leaderboard.</param>
public sealed record LeaderboardRankEntry(Guid LearnerId, long Score, int Rank);

/// <summary>
/// Fast-read leaderboard store: one ranked set per CEFR level (PROJECT-SPEC Faza 5 "future
/// leaderboards", the natural extension of the Redis Sorted Set already used for streaks).
/// Redis-backed in production, in-memory for dev/tests.
/// </summary>
public interface ILeaderboardStore
{
    /// <summary>
    /// Replaces every CEFR leaderboard from the durable lifetime-XP snapshot. This makes Redis
    /// a rebuildable read index rather than an independent source of truth.
    /// </summary>
    Task ReplaceAllAsync(
        IReadOnlyDictionary<CefrLevel, IReadOnlyDictionary<Guid, long>> scoresByLevel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets (or updates) the learner's score on <paramref name="level"/>'s leaderboard. Called
    /// after every points award and whenever the learner's CEFR level changes.
    /// </summary>
    Task SetScoreAsync(Guid learnerId, CefrLevel level, long lifetimeXp, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the learner from <paramref name="level"/>'s leaderboard - used when they advance
    /// to a new CEFR level, so they don't linger on the cohort they left.
    /// </summary>
    Task RemoveAsync(Guid learnerId, CefrLevel level, CancellationToken cancellationToken);

    /// <summary>The top <paramref name="count"/> ranked entries for a level, highest score first.</summary>
    Task<IReadOnlyList<LeaderboardRankEntry>> GetTopAsync(
        CefrLevel level, int count, CancellationToken cancellationToken);

    /// <summary>The learner's own score and 1-based rank on a level's leaderboard, or null if absent.</summary>
    Task<LeaderboardRankEntry?> GetLearnerRankAsync(
        Guid learnerId, CefrLevel level, CancellationToken cancellationToken);
}
