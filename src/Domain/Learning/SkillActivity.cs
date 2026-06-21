namespace Domain.Learning;

/// <summary>
/// One scored practice or assessment outcome contributing to a skill's score.
/// Recorded by any learning module (Speaking, Reading, ...) as the learner works.
/// The <see cref="OccurredAt"/> timestamp drives the exponential-decay weighting
/// in <see cref="SkillScoreCalculator"/> (PROJECT-SPEC G.4).
/// </summary>
public sealed class SkillActivity
{
    // Parameterless ctor for EF Core materialization.
    private SkillActivity()
    {
    }

    public SkillActivity(SkillType skill, int score, DateTimeOffset occurredAt)
    {
        if (score is < 0 or > 100)
            throw new Domain.Common.DomainException("Skill activity score must be between 0 and 100.");

        Id = Guid.NewGuid();
        Skill = skill;
        Score = score;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public SkillType Skill { get; private set; }

    /// <summary>The 0-100 outcome of this activity.</summary>
    public int Score { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
