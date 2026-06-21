using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Referral;
using Domain.Gamification;
using Domain.Learning;

namespace Application.Gamification;

/// <summary>
/// Records that a learner practiced one of the six core skills today, feeding the Home
/// "kunlik reja" 6-skill plan and the daily-goal streak (PROJECT-SPEC Faza 5). This is the
/// single place every skill-completion path funnels through so the streak advances on genuine
/// skill variety, no matter which module the learner finished.
/// </summary>
public interface IDailyProgressRecorder
{
    /// <summary>
    /// Marks <paramref name="skill"/> as practiced today for the learner (idempotent per
    /// learner/day/skill) and marks the day active so it counts toward the streak - a single
    /// completed activity keeps the streak alive. A no-op for an unidentified learner.
    /// </summary>
    Task<SkillRewardDto> RecordSkillAsync(
        Guid learnerId, SkillType skill, int scorePercent, DateTimeOffset now, CancellationToken cancellationToken);
}

/// <inheritdoc />
public sealed class DailyProgressRecorder : IDailyProgressRecorder
{
    private readonly IGamificationStore _store;
    private readonly IReferralService _referrals;
    private readonly IPointsService _points;

    public DailyProgressRecorder(IGamificationStore store, IReferralService referrals, IPointsService points)
    {
        _store = store;
        _referrals = referrals;
        _points = points;
    }

    public async Task<SkillRewardDto> RecordSkillAsync(
        Guid learnerId, SkillType skill, int scorePercent, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (learnerId == Guid.Empty)
            return SkillRewardDto.NotAwarded();

        // Completing a skill is the referral qualification signal: if this learner was referred,
        // their genuine engagement now qualifies the referral and rewards both sides (once). It is
        // safe and cheap to call on every completion - a no-op unless a pending referral exists.
        await _referrals.TryQualifyAsync(learnerId, now, cancellationToken);

        // Record against the learner's local day (UTC+5) so the daily plan and streak roll over at
        // local midnight, consistent with how the Home dashboard reads them back.
        var today = now.ToLocalDate();

        // Each distinct skill counts once per day toward the daily goal, so repeating a single
        // module does not inflate progress or the streak (PROJECT-SPEC Faza 5).
        var before = await _store.GetSkillsPracticedTodayAsync(learnerId, today, cancellationToken);
        if (before.Contains(skill))
        {
            var completedDays = await _store.GetCompletedDaysAsync(learnerId, cancellationToken);
            var streak = StreakCalculator.Compute(completedDays, today);
            return SkillRewardDto.NotAwarded(alreadyCreditedToday: true, streak.CurrentStreak);
        }

        await _store.MarkSkillPracticedAsync(learnerId, today, skill, cancellationToken);

        // A day counts toward the streak as soon as the learner completes at least one activity
        // (the widely-expected "did something today" streak). The full daily goal (three distinct
        // skills) stays a separate indicator surfaced via GamificationStatusDto.IsGoalMet, so the
        // streak reflects genuine daily engagement rather than requiring the whole plan every day.
        await _store.IncrementTaskCountAsync(learnerId, today, cancellationToken);
        await _store.MarkDayCompletedAsync(learnerId, today, cancellationToken);

        // Points/leaderboard feature: this is a genuine first-today completion of `skill`
        // (guarded by the `before.Contains(skill)` check above), so award points and refresh
        // the learner's leaderboard entry. `before` is the skill set prior to this completion.
        return await _points.AwardForActivityAsync(learnerId, scorePercent, before, now, cancellationToken);
    }
}
