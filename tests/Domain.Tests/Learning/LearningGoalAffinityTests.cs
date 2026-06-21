using Domain.Common;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Learning;

public class LearningGoalAffinityTests
{
    [Theory]
    [InlineData(LearningGoal.IeltsCefr)]
    [InlineData(LearningGoal.Work)]
    [InlineData(LearningGoal.Migration)]
    [InlineData(LearningGoal.Travel)]
    [InlineData(LearningGoal.GeneralSpeaking)]
    [InlineData(LearningGoal.School)]
    public void Every_real_goal_prefers_at_least_one_category(LearningGoal goal)
    {
        LearningGoalAffinity.PreferredCategories(goal).Should().NotBeEmpty();
    }

    [Fact]
    public void Unspecified_prefers_nothing_so_ordering_is_neutral()
    {
        LearningGoalAffinity.PreferredCategories(LearningGoal.Unspecified).Should().BeEmpty();
        LearningGoalAffinity.Weight(LearningGoal.Unspecified, "work_jobs").Should().Be(0);
        LearningGoalAffinity.IsRelevant(LearningGoal.Unspecified, "work_jobs").Should().BeFalse();
    }

    [Fact]
    public void Weight_is_highest_for_the_first_preferred_category_and_decreases()
    {
        var prefs = LearningGoalAffinity.PreferredCategories(LearningGoal.Work);

        var first = LearningGoalAffinity.Weight(LearningGoal.Work, prefs[0]);
        var second = LearningGoalAffinity.Weight(LearningGoal.Work, prefs[1]);

        first.Should().Be(prefs.Count);
        first.Should().BeGreaterThan(second);
    }

    [Fact]
    public void Weight_is_zero_for_a_category_the_goal_does_not_prefer()
    {
        // A travel-only category should be irrelevant to a Work goal.
        LearningGoalAffinity.Weight(LearningGoal.Work, "travel_transport").Should().Be(0);
    }

    [Fact]
    public void IsRelevant_matches_a_preferred_category()
    {
        LearningGoalAffinity.IsRelevant(LearningGoal.Travel, "travel_transport").Should().BeTrue();
    }
}
