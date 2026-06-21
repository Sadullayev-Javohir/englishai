using Domain.Retention;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Retention;

/// <summary>
/// Unit tests for the win-back staging ladder (PROJECT-SPEC I.2).
/// </summary>
public class WinBackStageTests
{
    [Theory]
    [InlineData(0, InactivityStage.Active)]
    [InlineData(2, InactivityStage.Active)]
    [InlineData(3, InactivityStage.Day3)]
    [InlineData(6, InactivityStage.Day3)]
    [InlineData(7, InactivityStage.Day7)]
    [InlineData(13, InactivityStage.Day7)]
    [InlineData(14, InactivityStage.Day14)]
    [InlineData(29, InactivityStage.Day14)]
    [InlineData(30, InactivityStage.Day30)]
    [InlineData(59, InactivityStage.Day30)]
    [InlineData(60, InactivityStage.Day60Plus)]
    [InlineData(365, InactivityStage.Day60Plus)]
    public void Maps_days_inactive_to_the_right_stage(int days, InactivityStage expected)
    {
        WinBackStage.ForDaysInactive(days).Should().Be(expected);
    }

    [Fact]
    public void Negative_days_inactive_throws()
    {
        var act = () => WinBackStage.ForDaysInactive(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Active_stage_has_no_notification_code()
    {
        WinBackStage.CodeFor(InactivityStage.Active).Should().BeNull();
    }

    [Theory]
    [InlineData(InactivityStage.Day3, "notify.winback_day3")]
    [InlineData(InactivityStage.Day7, "notify.winback_day7")]
    [InlineData(InactivityStage.Day14, "notify.winback_day14")]
    [InlineData(InactivityStage.Day30, "notify.winback_day30")]
    [InlineData(InactivityStage.Day60Plus, "notify.winback_day60")]
    public void Each_active_stage_has_its_template_code(InactivityStage stage, string code)
    {
        WinBackStage.CodeFor(stage).Should().Be(code);
    }
}
