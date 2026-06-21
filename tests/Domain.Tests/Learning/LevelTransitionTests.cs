using Domain.Assessment;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Learning;

public class LevelTransitionTests
{
    private static IEnumerable<SkillScore> Scores(params double[] values) =>
        values.Select((v, i) => new SkillScore((SkillType)(i + 1), v, 3)).ToList();

    [Fact]
    public void Eligible_when_four_skills_mastered_and_confirmation_passed()
    {
        var scores = Scores(85, 82, 90, 80, 50, 40); // four >= 80

        var status = LevelTransition.Evaluate(CefrLevel.B1, scores, confirmationTestPassed: true);

        status.IsEligible.Should().BeTrue();
        status.MasteredSkillCount.Should().Be(4);
    }

    [Fact]
    public void Not_eligible_without_confirmation_test()
    {
        var scores = Scores(85, 82, 90, 80, 88, 81); // six >= 80

        var status = LevelTransition.Evaluate(CefrLevel.B1, scores, confirmationTestPassed: false);

        status.IsEligible.Should().BeFalse();
        status.ConfirmationTestPassed.Should().BeFalse();
    }

    [Fact]
    public void Not_eligible_with_fewer_than_four_mastered_skills()
    {
        var scores = Scores(85, 82, 90, 50, 50, 40); // three >= 80

        var status = LevelTransition.Evaluate(CefrLevel.B1, scores, confirmationTestPassed: true);

        status.IsEligible.Should().BeFalse();
        status.MasteredSkillCount.Should().Be(3);
    }

    [Fact]
    public void Never_eligible_at_the_maximum_level()
    {
        var scores = Scores(95, 95, 95, 95, 95, 95);

        var status = LevelTransition.Evaluate(CefrLevel.C2, scores, confirmationTestPassed: true);

        status.IsEligible.Should().BeFalse();
        status.AtMaxLevel.Should().BeTrue();
    }

    [Fact]
    public void Not_eligible_when_a_productive_skill_is_below_the_minimum_floor()
    {
        var scores = new[]
        {
            new SkillScore(SkillType.Vocabulary, 90, 3), new SkillScore(SkillType.Grammar, 90, 3),
            new SkillScore(SkillType.Reading, 90, 3), new SkillScore(SkillType.Listening, 90, 3),
            new SkillScore(SkillType.Speaking, 59, 3), new SkillScore(SkillType.Writing, 85, 3)
        };

        var status = LevelTransition.Evaluate(CefrLevel.B1, scores, true);

        status.IsEligible.Should().BeFalse();
        status.ProductiveSkillsMeetFloor.Should().BeFalse();
    }

    [Fact]
    public void Not_eligible_when_scores_have_too_few_recent_samples()
    {
        var scores = Enum.GetValues<SkillType>().Select(s => new SkillScore(s, 95, 1));

        var status = LevelTransition.Evaluate(CefrLevel.B1, scores, true);

        status.IsEligible.Should().BeFalse();
        status.HasSufficientSamples.Should().BeFalse();
    }
}
