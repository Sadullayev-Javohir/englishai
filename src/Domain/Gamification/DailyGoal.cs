using Domain.Common;

namespace Domain.Gamification;

/// <summary>
/// The number of learning tasks a learner must complete in a single day to keep their
/// streak alive (PROJECT-SPEC Faza 5, "kunlik maqsad"). A small, achievable target keeps
/// the first-week "win streak" easy (PROJECT-SPEC E.2). The default is used until
/// per-learner customization is introduced.
/// </summary>
public sealed record DailyGoal
{
    /// <summary>Default daily target - three quick tasks, deliberately low to build the habit.</summary>
    public const int DefaultTargetTasks = 3;

    public DailyGoal(int targetTasks)
    {
        if (targetTasks < 1)
            throw new DomainException("Daily goal must require at least one task.");

        TargetTasks = targetTasks;
    }

    public int TargetTasks { get; }

    public static DailyGoal Default => new(DefaultTargetTasks);

    /// <summary>Whether the given number of completed tasks meets the goal for the day.</summary>
    public bool IsMet(int completedTasks) => completedTasks >= TargetTasks;
}
