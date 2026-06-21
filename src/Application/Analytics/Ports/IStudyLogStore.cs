using Domain.Analytics;
using Domain.Learning;

namespace Application.Analytics.Ports;

/// <summary>
/// Durable persistence port for per-day study time (PROJECT-SPEC Faza 2 analitika). Backed by
/// EF Core/PostgreSQL in production so the year/all-time history survives - unlike the Redis
/// gamification counters, which expire. An in-memory adapter keeps the app runnable in dev/tests.
/// </summary>
public interface IStudyLogStore
{
    /// <summary>
    /// Credits <paramref name="seconds"/> of study time for a skill to the learner's
    /// (learner, day) row, creating the row on first study that day. Idempotent only in the
    /// sense of being safe to call repeatedly - each call accumulates.
    /// </summary>
    Task AddStudyTimeAsync(
        Guid learnerId, DateOnly day, SkillType skill, int seconds, CancellationToken cancellationToken);

    /// <summary>
    /// Computes the progress-dashboard study statistics without transferring the learner's entire
    /// daily history to the application. Implementations may retain only the bounded chart window
    /// while aggregating all-time scalar totals in storage.
    /// </summary>
    Task<StudyStats> GetStatsAsync(Guid learnerId, DateOnly today, CancellationToken cancellationToken);

    /// <summary>
    /// Every (learner, day) that recorded study time on or after <paramref name="fromDay"/>, across
    /// the whole user base, projected to just learner + day. Powers the founder analytics dashboard
    /// (DAU/WAU/MAU and cohort retention) in a single read. Callers pass a bounded <paramref
    /// name="fromDay"/> (e.g. the retention look-back window) so the scan stays proportional to the
    /// reporting window, not all history.
    /// </summary>
    Task<IReadOnlyList<StudyActivityDay>> GetActivityDaysAsync(
        DateOnly fromDay, CancellationToken cancellationToken);

    /// <summary>
    /// Aggregate study totals across the whole learner base. Used only for anonymous social-proof
    /// reporting, so implementations return sums rather than learner-level rows.
    /// </summary>
    Task<GlobalStudyTotals> GetGlobalTotalsAsync(CancellationToken cancellationToken);
}

public sealed record GlobalStudyTotals(long TotalSeconds, long SpeakingSeconds);
