namespace Domain.Gamification;

/// <summary>
/// A point-in-time view of a learner's streak (PROJECT-SPEC Faza 5 gamification). A
/// "streak" is the run of consecutive days on which the daily goal was met.
/// </summary>
/// <param name="CurrentStreak">
/// Length of the unbroken run ending today (if today's goal is met) or yesterday (if
/// today is not yet met but the run is still alive and can be extended today). Zero when
/// neither today nor yesterday was completed.
/// </param>
/// <param name="LongestStreak">The longest consecutive run the learner has ever achieved.</param>
/// <param name="IsCompletedToday">Whether today's daily goal has already been met.</param>
/// <param name="IsAtRisk">
/// True when a live streak will be lost unless the goal is completed today (i.e. there is
/// a current streak but today is not yet done).
/// </param>
public sealed record StreakSnapshot(
    int CurrentStreak,
    int LongestStreak,
    bool IsCompletedToday,
    bool IsAtRisk)
{
    public static StreakSnapshot Empty { get; } = new(0, 0, false, false);
}
