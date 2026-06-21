namespace Domain.Learning;

/// <summary>
/// Outcome of evaluating the CEFR level-up criteria for a learner.
/// </summary>
public sealed record LevelTransitionStatus(
    bool IsEligible,
    int MasteredSkillCount,
    int RequiredMasteredSkills,
    bool ConfirmationTestPassed,
    bool AtMaxLevel,
    bool ProductiveSkillsMeetFloor = true,
    bool HasSufficientSamples = true);

/// <summary>
/// Pure evaluation of the PROJECT-SPEC G.4 level-up rule: a learner rises a CEFR
/// level only when at least four skill scores reach the mastery threshold AND a
/// fresh placement-style confirmation mini-test has been passed. Daily practice
/// scores alone are never enough - the confirmation test is the "prove it" gate.
/// Automatic level *demotion* is deliberately not modelled here (demotivation risk).
/// </summary>
public static class LevelTransition
{
    /// <summary>
    /// Skill-score level a skill must reach to count as mastered for advancement
    /// (PROJECT-SPEC G.4 example: 80/100). Applied uniformly across levels.
    /// </summary>
    public const double MasteryThreshold = 80.0;

    /// <summary>At least four of the six skills must be mastered (PROJECT-SPEC G.4).</summary>
    public const int RequiredMasteredSkills = 4;
    public const double ProductiveSkillFloor = 60.0;
    public const int MinimumSamplesPerSkill = 2;

    public static LevelTransitionStatus Evaluate(
        Domain.Assessment.CefrLevel currentLevel,
        IEnumerable<SkillScore> skillScores,
        bool confirmationTestPassed)
    {
        var scores = skillScores.ToArray();
        var masteredCount = scores.Count(s => s.Score >= MasteryThreshold);
        var atMaxLevel = currentLevel >= Domain.Assessment.CefrLevelExtensions.Ceiling;
        var productiveSkillsMeetFloor = new[] { SkillType.Speaking, SkillType.Writing }
            .All(skill => scores.Any(s => s.Skill == skill && s.Score >= ProductiveSkillFloor));
        var hasSufficientSamples = scores.Count(s => s.SampleCount >= MinimumSamplesPerSkill)
            >= RequiredMasteredSkills;

        var isEligible =
            !atMaxLevel &&
            masteredCount >= RequiredMasteredSkills &&
            productiveSkillsMeetFloor &&
            hasSufficientSamples &&
            confirmationTestPassed;

        return new LevelTransitionStatus(
            isEligible,
            masteredCount,
            RequiredMasteredSkills,
            confirmationTestPassed,
            atMaxLevel,
            productiveSkillsMeetFloor,
            hasSufficientSamples);
    }
}
