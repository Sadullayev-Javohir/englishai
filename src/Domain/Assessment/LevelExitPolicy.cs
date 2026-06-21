using Domain.Learning;

namespace Domain.Assessment;

public sealed record LevelExitRequirements(
    CefrLevel TestedLevel,
    CefrLevel TargetLevel,
    double MinimumOverallScore,
    double MinimumStageScore,
    double ProductiveStageFloor,
    int RequiredMasteredSkills,
    double MasteryThreshold,
    double ProductiveSkillFloor,
    int MinimumSamplesPerSkill);

public sealed record LevelExitEvaluation(
    bool Passed,
    double MinimumOverallScore,
    double MinimumStageScore,
    double ProductiveStageFloor,
    IReadOnlyList<TestStage> FailedStages);

public static class LevelExitPolicy
{
    public static LevelExitRequirements RequirementsFor(CefrLevel testedLevel)
    {
        if (testedLevel >= CefrLevelExtensions.Ceiling)
            throw new ArgumentOutOfRangeException(nameof(testedLevel), testedLevel, "C2 has no exit test.");

        var (overall, stage, productive) = testedLevel switch
        {
            CefrLevel.A1 => (70.0, 55.0, 50.0),
            CefrLevel.A2 => (72.0, 58.0, 58.0),
            CefrLevel.B1 => (75.0, 60.0, 65.0),
            CefrLevel.B2 => (78.0, 65.0, 70.0),
            CefrLevel.C1 => (82.0, 70.0, 75.0),
            _ => throw new ArgumentOutOfRangeException(nameof(testedLevel), testedLevel, "Unknown CEFR level.")
        };

        return new LevelExitRequirements(
            testedLevel,
            testedLevel.StepUp(),
            overall,
            stage,
            productive,
            LevelTransition.RequiredMasteredSkills,
            LevelTransition.MasteryThreshold,
            LevelTransition.ProductiveSkillFloor,
            LevelTransition.MinimumSamplesPerSkill);
    }

    public static LevelExitEvaluation Evaluate(CefrLevel testedLevel, PlacementResult result)
    {
        var requirements = RequirementsFor(testedLevel);
        var failedStages = result.StageResults.Values
            .Where(stage => stage.Score < RequiredStageScore(requirements, stage.Stage))
            .Select(stage => stage.Stage)
            .OrderBy(stage => stage)
            .ToList();

        failedStages.AddRange(
            Enum.GetValues<TestStage>().Where(stage => !result.StageResults.ContainsKey(stage)));

        return new LevelExitEvaluation(
            result.OverallScore >= requirements.MinimumOverallScore && failedStages.Count == 0,
            requirements.MinimumOverallScore,
            requirements.MinimumStageScore,
            requirements.ProductiveStageFloor,
            failedStages.Distinct().OrderBy(stage => stage).ToList());
    }

    private static double RequiredStageScore(LevelExitRequirements requirements, TestStage stage) =>
        stage is TestStage.Writing or TestStage.Speaking
            ? requirements.ProductiveStageFloor
            : requirements.MinimumStageScore;
}
