using Domain.Common;
using Domain.Gamification;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Gamification;

public class DailyGoalTests
{
    [Fact]
    public void Default_goal_uses_the_default_target()
    {
        DailyGoal.Default.TargetTasks.Should().Be(DailyGoal.DefaultTargetTasks);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    public void Is_met_when_completed_tasks_reach_the_target(int completed, bool expected)
    {
        new DailyGoal(3).IsMet(completed).Should().Be(expected);
    }

    [Fact]
    public void Target_below_one_is_rejected()
    {
        var act = () => new DailyGoal(0);

        act.Should().Throw<DomainException>();
    }
}
