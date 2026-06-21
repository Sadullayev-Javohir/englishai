using Domain.Gamification;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Gamification;

public class PointsPolicyTests
{
    [Fact]
    public void Scores_below_the_mastery_gate_earn_no_xp()
    {
        PointsPolicy.ActivityXpForScore(74).Should().Be(0);
    }

    [Theory]
    [InlineData(75, 10)]
    [InlineData(84, 10)]
    [InlineData(85, 15)]
    [InlineData(94, 15)]
    [InlineData(95, 20)]
    [InlineData(100, 20)]
    public void Qualifying_scores_use_quality_bands(int score, int expectedXp)
    {
        PointsPolicy.ActivityXpForScore(score).Should().Be(expectedXp);
    }

    [Fact]
    public void Streak_milestones_are_ordered_ascending_by_days()
    {
        PointsPolicy.StreakMilestones.Select(m => m.Days).Should().BeInAscendingOrder();
    }
}
