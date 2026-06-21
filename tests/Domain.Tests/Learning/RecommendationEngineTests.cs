using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Learning;

public class RecommendationEngineTests
{
    private static readonly LevelTransitionStatus NotEligible =
        new(IsEligible: false, MasteredSkillCount: 1, RequiredMasteredSkills: 4,
            ConfirmationTestPassed: false, AtMaxLevel: false);

    private static readonly LevelTransitionStatus Eligible =
        new(IsEligible: true, MasteredSkillCount: 4, RequiredMasteredSkills: 4,
            ConfirmationTestPassed: true, AtMaxLevel: false);

    [Fact]
    public void Recommends_focusing_on_the_weakest_skill()
    {
        var scores = new[]
        {
            new SkillScore(SkillType.Speaking, 30, 2),
            new SkillScore(SkillType.Reading, 80, 2)
        };

        var result = RecommendationEngine.Recommend(
            scores, new Dictionary<ErrorCategory, int>(), NotEligible);

        result.Should().ContainSingle(r => r.Code == RecommendationEngine.FocusSkillCode)
            .Which.Skill.Should().Be(SkillType.Speaking);
    }

    [Fact]
    public void Recommends_fixing_the_most_frequent_error()
    {
        var heatmap = new Dictionary<ErrorCategory, int>
        {
            [ErrorCategory.Articles] = 7,
            [ErrorCategory.Prepositions] = 2
        };

        var result = RecommendationEngine.Recommend(
            Array.Empty<SkillScore>(), heatmap, NotEligible);

        result.Should().ContainSingle(r => r.Code == RecommendationEngine.FixErrorCode)
            .Which.Category.Should().Be(ErrorCategory.Articles);
    }

    [Fact]
    public void Confirmation_test_recommendation_comes_first_when_eligible()
    {
        var scores = new[] { new SkillScore(SkillType.Speaking, 30, 2) };

        var result = RecommendationEngine.Recommend(
            scores, new Dictionary<ErrorCategory, int>(), Eligible);

        result.First().Code.Should().Be(RecommendationEngine.TakeConfirmationTestCode);
    }

    [Fact]
    public void Returns_nothing_for_a_blank_profile()
    {
        var result = RecommendationEngine.Recommend(
            Array.Empty<SkillScore>(), new Dictionary<ErrorCategory, int>(), NotEligible);

        result.Should().BeEmpty();
    }
}
