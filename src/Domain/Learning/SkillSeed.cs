namespace Domain.Learning;

/// <summary>
/// Baseline 0-100 score for a skill, derived from the placement test. It is the
/// fallback a skill's score returns to when there is no recent activity, so a fresh
/// or idle learner still shows a meaningful level (see <see cref="SkillScoreCalculator"/>).
/// </summary>
public sealed class SkillSeed
{
    // Parameterless ctor for EF Core materialization.
    private SkillSeed()
    {
    }

    public SkillSeed(SkillType skill, double score)
    {
        Id = Guid.NewGuid();
        Skill = skill;
        Score = score;
    }

    public Guid Id { get; private set; }
    public SkillType Skill { get; private set; }
    public double Score { get; private set; }

    internal void UpdateScore(double score) => Score = score;
}
