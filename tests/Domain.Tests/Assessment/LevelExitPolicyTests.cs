using Domain.Assessment;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Assessment;

public class LevelExitPolicyTests
{
    [Theory]
    [InlineData(CefrLevel.A1, 70, 55, 50)]
    [InlineData(CefrLevel.A2, 72, 58, 58)]
    [InlineData(CefrLevel.B1, 75, 60, 65)]
    [InlineData(CefrLevel.B2, 78, 65, 70)]
    [InlineData(CefrLevel.C1, 82, 70, 75)]
    public void Defines_level_specific_requirements(
        CefrLevel level,
        double overall,
        double stage,
        double productive)
    {
        var requirements = LevelExitPolicy.RequirementsFor(level);

        requirements.MinimumOverallScore.Should().Be(overall);
        requirements.MinimumStageScore.Should().Be(stage);
        requirements.ProductiveStageFloor.Should().Be(productive);
        requirements.TargetLevel.Should().Be(level.StepUp());
    }

    [Fact]
    public void A1_no_longer_passes_with_a_low_result()
    {
        LevelExitPolicy.Evaluate(CefrLevel.A1, Result(20, 20)).Passed.Should().BeFalse();
    }

    [Fact]
    public void Overall_score_cannot_hide_a_weak_skill()
    {
        var stages = StrongStages(90);
        stages[TestStage.Speaking] = new StageResult(TestStage.Speaking, CefrLevel.A2, 40);

        var evaluation = LevelExitPolicy.Evaluate(
            CefrLevel.B1,
            new PlacementResult(CefrLevel.C1, 82, stages));

        evaluation.Passed.Should().BeFalse();
        evaluation.FailedStages.Should().Contain(TestStage.Speaking);
    }

    [Fact]
    public void Passing_requires_all_six_stages()
    {
        var stages = StrongStages(90);
        stages.Remove(TestStage.Speaking);

        LevelExitPolicy.Evaluate(
                CefrLevel.A2,
                new PlacementResult(CefrLevel.C1, 90, stages))
            .FailedStages.Should().Contain(TestStage.Speaking);
    }

    private static PlacementResult Result(double overall, int stageScore) =>
        new(CefrLevel.A1, overall, StrongStages(stageScore));

    private static Dictionary<TestStage, StageResult> StrongStages(int score) =>
        Enum.GetValues<TestStage>().ToDictionary(
            stage => stage,
            stage => new StageResult(stage, CefrLevelExtensions.FromScore(score), score));
}
