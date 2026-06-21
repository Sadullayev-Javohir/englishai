using Domain.Learning;

namespace Application.Gamification.Ports;

/// <summary>
/// Persistence port for daily-goal progress and streak history (PROJECT-SPEC Faza 5).
/// In production this is backed by Redis - a per-day counter for today's task count and a
/// Sorted Set of completed days per learner - so leaderboard/streak reads stay fast. An
/// in-memory adapter keeps the app runnable in dev/tests without Redis.
/// </summary>
public interface IGamificationStore
{
    /// <summary>Increments the learner's completed-task count for the given day and returns the new total.</summary>
    Task<int> IncrementTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken);

    /// <summary>Returns how many tasks the learner has completed on the given day.</summary>
    Task<int> GetTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken);

    /// <summary>
    /// Records that the learner practiced one of the six core skills today (the "kunlik reja"
    /// 6-skill plan on the Home dashboard). Idempotent per (learner, day, skill): practicing the
    /// same skill twice in a day is a no-op. Returns the distinct set of skills practiced today.
    /// </summary>
    Task<IReadOnlyCollection<SkillType>> MarkSkillPracticedAsync(
        Guid learnerId, DateOnly day, SkillType skill, CancellationToken cancellationToken);

    /// <summary>The distinct core skills the learner has practiced on the given day.</summary>
    Task<IReadOnlyCollection<SkillType>> GetSkillsPracticedTodayAsync(
        Guid learnerId, DateOnly day, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a day as one on which the learner met their daily goal. Idempotent: marking
    /// the same day twice has no additional effect (a day is either completed or not).
    /// </summary>
    Task MarkDayCompletedAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken);

    /// <summary>All days on which the learner met their daily goal (used to compute streaks).</summary>
    Task<IReadOnlyCollection<DateOnly>> GetCompletedDaysAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes all gamification state for a learner (streak history + daily counters/skills).
    /// Called when an account is deleted so no personal progress lingers in the cache.
    /// </summary>
    Task DeleteLearnerAsync(Guid learnerId, CancellationToken cancellationToken);
}
