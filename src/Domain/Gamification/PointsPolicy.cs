namespace Domain.Gamification;

/// <summary>A one-time bonus unlocked the first time a streak reaches <see cref="Days"/>.</summary>
public sealed record StreakMilestone(int Days, int BonusPoints);

/// <summary>
/// The points economy (leaderboard/points feature): how many XP each qualified learning event
/// is worth. Pure and stateless so it is fully unit-testable;
/// <see cref="Application.Gamification.PointsService"/> (Application layer) is the only caller.
/// </summary>
public static class PointsPolicy
{
    public const int MinimumQualifyingScore = 75;
    public const int PassedActivityXp = 10;
    public const int StrongActivityXp = 15;
    public const int ExcellentActivityXp = 20;
    public const int ModuleCompletionPoints = PassedActivityXp;

    /// <summary>Awarded once per day for the learner's first activity of that day.</summary>
    public const int DailyActivityBonusPoints = 5;

    /// <summary>Awarded once per day when all six core modules have been practiced that day.</summary>
    public const int AllModulesBonusPoints = 25;

    /// <summary>Streak-length milestones, each awarded exactly once (ascending order).</summary>
    public static readonly IReadOnlyList<StreakMilestone> StreakMilestones = new[]
    {
        new StreakMilestone(7, 50),
        new StreakMilestone(30, 200),
        new StreakMilestone(100, 500),
    };

    public static int ActivityXpForScore(int scorePercent)
    {
        var score = Math.Clamp(scorePercent, 0, 100);
        return score switch
        {
            >= 95 => ExcellentActivityXp,
            >= 85 => StrongActivityXp,
            >= MinimumQualifyingScore => PassedActivityXp,
            _ => 0,
        };
    }
}
