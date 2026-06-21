namespace Domain.Gamification;

/// <summary>
/// The energy budget that limits how many Video lessons and Speaking conversations a learner
/// can <em>start</em> (PROJECT-SPEC, energy flow 01-09): five units, one unit back every
/// 36 minutes, so an empty bar is full again after exactly three hours. Energy regenerates
/// one unit at a time - never in a single hourly burst - and the clock stops at the maximum.
/// </summary>
public static class EnergyPolicy
{
    public const int MaximumEnergy = 5;

    /// <summary>Wall-clock time it takes to earn a single unit back.</summary>
    public static readonly TimeSpan RefillInterval = TimeSpan.FromMinutes(36);

    /// <summary>Time from an empty bar to a full one: 5 x 36 minutes = 3 hours.</summary>
    public static TimeSpan FullRefillDuration => RefillInterval * MaximumEnergy;
}
