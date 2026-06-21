using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Subscription.Access;
using Domain.Assessment;
using Domain.Gamification;
using Domain.Learning;

namespace Application.Gamification;

/// <summary>
/// Awards leaderboard/points-feature points for genuine daily engagement and keeps the
/// learner's Redis leaderboard entry in sync. Called by <see cref="DailyProgressRecorder"/>
/// right after it records a first-today skill completion, so every one of the six modules
/// feeds points without any module handler needing to know about this feature.
/// </summary>
public interface IPointsService
{
    /// <summary>
    /// Awards points for completing <paramref name="skillsPracticedBeforeToday"/> + one more
    /// skill just now: a score-based module award, plus the first-activity-of-day and
    /// all-six-modules bonuses when applicable, plus any newly-crossed streak milestone. Premium
    /// status does not multiply XP. Also pushes the learner's updated
    /// lifetime score onto their current CEFR level's leaderboard.
    /// </summary>
    Task<SkillRewardDto> AwardForActivityAsync(
        Guid learnerId,
        int scorePercent,
        IReadOnlyCollection<SkillType> skillsPracticedBeforeToday,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <inheritdoc />
public sealed class PointsService : IPointsService
{
    private readonly ILearnerPointsRepository _points;
    private readonly ILeaderboardStore _leaderboard;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IGamificationStore _gamification;

    public PointsService(
        ILearnerPointsRepository points,
        ILeaderboardStore leaderboard,
        ILearnerProfileRepository profiles,
        IGamificationStore gamification)
    {
        _points = points;
        _leaderboard = leaderboard;
        _profiles = profiles;
        _gamification = gamification;
    }

    public async Task<SkillRewardDto> AwardForActivityAsync(
        Guid learnerId,
        int scorePercent,
        IReadOnlyCollection<SkillType> skillsPracticedBeforeToday,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var activityXp = PointsPolicy.ActivityXpForScore(scorePercent);
        if (activityXp == 0)
            return SkillRewardDto.NotAwarded();

        var dailyBonus = skillsPracticedBeforeToday.Count == 0
            ? PointsPolicy.DailyActivityBonusPoints
            : 0;
        var allModulesBonus = skillsPracticedBeforeToday.Count + 1 == Enum.GetValues<SkillType>().Length
            ? PointsPolicy.AllModulesBonusPoints
            : 0;

        var learnerPoints = await _points.GetOrCreateAsync(learnerId, now, cancellationToken);

        var completedDays = await _gamification.GetCompletedDaysAsync(learnerId, cancellationToken);
        var streak = StreakCalculator.Compute(completedDays, now.ToLocalDate());
        var milestoneBonus = learnerPoints.TryAwardStreakMilestone(streak.CurrentStreak, now) ?? 0;

        var total = activityXp + dailyBonus + allModulesBonus + milestoneBonus;
        learnerPoints.Earn(total, now);
        await _points.SaveAsync(learnerPoints, cancellationToken);

        var profile = await _profiles.GetByLearnerIdAsync(learnerId, cancellationToken);
        var level = profile?.OverallLevel ?? CefrLevel.A2;
        await _leaderboard.SetScoreAsync(learnerId, level, learnerPoints.LifetimeXp, cancellationToken);

        return new SkillRewardDto(
            total,
            total,
            activityXp,
            dailyBonus,
            allModulesBonus,
            milestoneBonus,
            PremiumMultiplierApplied: false,
            streak.CurrentStreak,
            AlreadyCreditedToday: false);
    }
}
