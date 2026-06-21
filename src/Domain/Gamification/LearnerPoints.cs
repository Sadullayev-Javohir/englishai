using Domain.Common;

namespace Domain.Gamification;

/// <summary>
/// A learner's durable points ledger: the leaderboard/points feature. Two counters share
/// every earn event but diverge on spend: <see cref="LifetimeXp"/> is the leaderboard score
/// and never decreases (so redeeming a discount never costs rank), while
/// <see cref="SpendableCoins"/> is the balance that funds Pro-discount redemptions and drops
/// when spent. Always persisted in Postgres - unlike the Redis-only streak counters, this
/// balance must never be lost.
/// </summary>
public sealed class LearnerPoints
{
    private LearnerPoints()
    {
    }

    private LearnerPoints(Guid learnerId, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Energy = EnergyPolicy.MaximumEnergy;
        EnergyRefilledAt = now;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public long LifetimeXp { get; private set; }
    public long SpendableCoins { get; private set; }
    public int Energy { get; private set; }

    /// <summary>
    /// Anchor for the regeneration clock: the moment the most recent unit was credited (or, at a
    /// full bar, simply "now", so waiting while full never banks time towards later refills).
    /// </summary>
    public DateTimeOffset EnergyRefilledAt { get; private set; }

    /// <summary>The longest streak-milestone (in days) already awarded, so each fires once.</summary>
    public int HighestStreakMilestoneReached { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static LearnerPoints CreateNew(Guid learnerId, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        return new LearnerPoints(learnerId, now);
    }

    /// <summary>Credits both counters. Used for every points award (module, streak, bonus, ...).</summary>
    public void Earn(int amount, DateTimeOffset now)
    {
        if (amount <= 0)
            throw new DomainException("Earned points must be a positive amount.");

        LifetimeXp += amount;
        SpendableCoins += amount;
        UpdatedAt = now;
    }

    /// <summary>
    /// Credits every whole <see cref="EnergyPolicy.RefillInterval"/> elapsed since the anchor,
    /// one unit at a time, capped at the maximum. Returns whether anything changed so callers
    /// can skip a pointless write.
    /// </summary>
    public bool RefreshEnergy(DateTimeOffset now)
    {
        if (Energy >= EnergyPolicy.MaximumEnergy)
        {
            // A full bar has no clock to run. Keep the anchor at "now" so the wait that follows
            // the next spend starts from that spend, not from whenever the bar last filled.
            if (EnergyRefilledAt >= now)
                return false;

            EnergyRefilledAt = now;
            UpdatedAt = now;
            return true;
        }

        var elapsed = now - EnergyRefilledAt;
        if (elapsed < EnergyPolicy.RefillInterval)
            return false;

        var earned = (int)(elapsed.Ticks / EnergyPolicy.RefillInterval.Ticks);
        var credited = Math.Min(earned, EnergyPolicy.MaximumEnergy - Energy);

        Energy += credited;
        EnergyRefilledAt = Energy >= EnergyPolicy.MaximumEnergy
            ? now
            : EnergyRefilledAt.Add(EnergyPolicy.RefillInterval * credited);
        UpdatedAt = now;
        return true;
    }

    /// <summary>
    /// Refills what is due, then spends one unit on starting a Video or Speaking activity.
    /// Returns false - without changing anything - when the bar is empty.
    /// </summary>
    public bool TryConsumeEnergy(DateTimeOffset now)
    {
        RefreshEnergy(now);
        if (Energy == 0)
            return false;

        // Spending from a full bar starts the regeneration clock at this moment.
        if (Energy == EnergyPolicy.MaximumEnergy)
            EnergyRefilledAt = now;

        Energy--;
        UpdatedAt = now;
        return true;
    }

    /// <summary>When the next single unit lands; null while the bar is full.</summary>
    public DateTimeOffset? NextEnergyRefillAt => Energy >= EnergyPolicy.MaximumEnergy
        ? null
        : EnergyRefilledAt.Add(EnergyPolicy.RefillInterval);

    /// <summary>When the bar reaches the maximum again; null while it already is.</summary>
    public DateTimeOffset? FullEnergyRefillAt => Energy >= EnergyPolicy.MaximumEnergy
        ? null
        : EnergyRefilledAt.Add(EnergyPolicy.RefillInterval * (EnergyPolicy.MaximumEnergy - Energy));

    /// <summary>
    /// If <paramref name="currentStreakDays"/> has newly crossed a milestone not yet awarded,
    /// records it and returns the bonus amount to earn; otherwise returns null. Does not call
    /// <see cref="Earn"/> itself so the caller can apply the Pro multiplier first.
    /// </summary>
    public int? TryAwardStreakMilestone(int currentStreakDays, DateTimeOffset now)
    {
        var milestone = PointsPolicy.StreakMilestones
            .Where(m => m.Days <= currentStreakDays && m.Days > HighestStreakMilestoneReached)
            .OrderByDescending(m => m.Days)
            .FirstOrDefault();

        if (milestone is null)
            return null;

        HighestStreakMilestoneReached = milestone.Days;
        UpdatedAt = now;
        return milestone.BonusPoints;
    }

    /// <summary>Spends coins on a discount redemption. Only <see cref="SpendableCoins"/> is debited.</summary>
    public void Redeem(int coinsCost, DateTimeOffset now)
    {
        if (coinsCost <= 0)
            throw new DomainException("Redemption cost must be a positive amount.");
        if (SpendableCoins < coinsCost)
            throw new DomainException("Insufficient coin balance for this redemption.");

        SpendableCoins -= coinsCost;
        UpdatedAt = now;
    }
}
