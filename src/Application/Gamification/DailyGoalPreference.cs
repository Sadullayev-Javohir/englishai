using Application.Identity.Ports;
using PreferenceGoal = Domain.Identity.DailyGoal;
using GoalTarget = Domain.Gamification.DailyGoal;

namespace Application.Gamification;

/// <summary>
/// Translates a learner's saved "Kunlik maqsad" preference (Xotirjam/Oddiy/Jadal) into the
/// concrete daily task target used by the streak/goal engine. Calm keeps the habit-forming bar
/// low; Intense pushes a fuller day. Without this the Profile setting would persist but never
/// change the actual goal.
/// </summary>
public static class DailyGoalPreference
{
    public const int CalmTargetTasks = 2;
    public const int NormalTargetTasks = 3;
    public const int IntenseTargetTasks = 5;

    /// <summary>Maps a saved preference to the concrete daily goal.</summary>
    public static GoalTarget ToGoal(PreferenceGoal preference) => preference switch
    {
        PreferenceGoal.Calm => new GoalTarget(CalmTargetTasks),
        PreferenceGoal.Intense => new GoalTarget(IntenseTargetTasks),
        _ => new GoalTarget(NormalTargetTasks),
    };

    /// <summary>
    /// Resolves the learner's active daily goal from their saved preferences, falling back to the
    /// default when they have never changed the setting.
    /// </summary>
    public static async Task<GoalTarget> ResolveAsync(
        IUserPreferencesStore preferences, Guid learnerId, CancellationToken cancellationToken)
    {
        var saved = await preferences.GetByUserIdAsync(learnerId, cancellationToken);
        return saved is null ? GoalTarget.Default : ToGoal(saved.DailyGoal);
    }
}
