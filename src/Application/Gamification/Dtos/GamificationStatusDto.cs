using Domain.Gamification;
using Domain.Learning;

namespace Application.Gamification.Dtos;

/// <summary>
/// The learner's daily-goal and streak status for the Home dashboard (PROJECT-SPEC Faza 5).
/// <see cref="SkillsCompletedToday"/> drives the "kunlik reja" 6-skill plan card - the
/// canonical names (e.g. <c>Vocabulary</c>) of the core skills practiced today.
/// </summary>
public sealed record GamificationStatusDto(
    int TodayCompletedTasks,
    int DailyGoalTarget,
    bool IsGoalMet,
    int CurrentStreak,
    int LongestStreak,
    bool IsStreakAtRisk,
    IReadOnlyList<string> SkillsCompletedToday)
{
    public static GamificationStatusDto From(
        int todayCompleted,
        DailyGoal goal,
        StreakSnapshot streak,
        IReadOnlyCollection<SkillType>? skillsCompletedToday = null) =>
        new(
            TodayCompletedTasks: todayCompleted,
            DailyGoalTarget: goal.TargetTasks,
            IsGoalMet: goal.IsMet(todayCompleted),
            CurrentStreak: streak.CurrentStreak,
            LongestStreak: streak.LongestStreak,
            IsStreakAtRisk: streak.IsAtRisk,
            SkillsCompletedToday: (skillsCompletedToday ?? Array.Empty<SkillType>())
                .Select(s => s.ToString())
                .ToList());
}
