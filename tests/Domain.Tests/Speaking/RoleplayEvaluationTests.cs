using Domain.Common;
using Domain.Speaking;
using FluentAssertions;

namespace Domain.Tests.Speaking;

public class RoleplayEvaluationTests
{
    [Fact]
    public void OverallScore_is_the_average_of_the_four_dimensions()
    {
        var evaluation = new RoleplayEvaluation(90, 80, 70, 60);

        evaluation.OverallScore.Should().Be(75.0);
    }

    [Fact]
    public void Band_is_Good_at_or_above_the_threshold()
    {
        var evaluation = new RoleplayEvaluation(80, 80, 80, 80);

        evaluation.Band.Should().Be(PronunciationBand.Good);
    }

    [Fact]
    public void Band_is_NeedsImprovement_below_the_threshold()
    {
        var evaluation = new RoleplayEvaluation(90, 80, 70, 60); // overall 75

        evaluation.Band.Should().Be(PronunciationBand.NeedsImprovement);
    }

    [Fact]
    public void Strongest_and_weakest_dimensions_are_identified()
    {
        var evaluation = new RoleplayEvaluation(
            taskCompletion: 90, fluency: 80, grammar: 70, appropriateness: 60);

        evaluation.StrongestDimension.Should().Be(RoleplayDimension.TaskCompletion);
        evaluation.WeakestDimension.Should().Be(RoleplayDimension.Appropriateness);
    }

    [Fact]
    public void Ties_are_broken_deterministically_by_dimension_order()
    {
        // All equal: strongest is the first dimension, weakest is the last - never random.
        var evaluation = new RoleplayEvaluation(70, 70, 70, 70);

        evaluation.StrongestDimension.Should().Be(RoleplayDimension.TaskCompletion);
        evaluation.WeakestDimension.Should().Be(RoleplayDimension.Appropriateness);
    }

    [Fact]
    public void ScoreFor_returns_the_matching_dimension_score()
    {
        var evaluation = new RoleplayEvaluation(90, 80, 70, 60);

        evaluation.ScoreFor(RoleplayDimension.Fluency).Should().Be(80);
    }

    [Theory]
    [InlineData(-1, 50, 50, 50)]
    [InlineData(50, 101, 50, 50)]
    [InlineData(50, 50, 50, 200)]
    public void Constructor_rejects_scores_outside_zero_to_hundred(
        double task, double fluency, double grammar, double appropriateness)
    {
        var act = () => new RoleplayEvaluation(task, fluency, grammar, appropriateness);

        act.Should().Throw<DomainException>();
    }
}
