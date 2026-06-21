using Domain.Assessment;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Assessment;

public class CefrLevelExtensionsTests
{
    [Theory]
    [InlineData(CefrLevel.A1, CefrLevel.A2)]
    [InlineData(CefrLevel.B1, CefrLevel.B2)]
    [InlineData(CefrLevel.C1, CefrLevel.C2)]
    public void StepUp_moves_one_level_harder(CefrLevel from, CefrLevel expected)
    {
        from.StepUp().Should().Be(expected);
    }

    [Fact]
    public void StepUp_is_clamped_at_C2()
    {
        CefrLevel.C2.StepUp().Should().Be(CefrLevel.C2);
    }

    [Theory]
    [InlineData(CefrLevel.C2, CefrLevel.C1)]
    [InlineData(CefrLevel.B2, CefrLevel.B1)]
    [InlineData(CefrLevel.A2, CefrLevel.A1)]
    public void StepDown_moves_one_level_easier(CefrLevel from, CefrLevel expected)
    {
        from.StepDown().Should().Be(expected);
    }

    [Fact]
    public void StepDown_is_clamped_at_A1()
    {
        CefrLevel.A1.StepDown().Should().Be(CefrLevel.A1);
    }

    [Theory]
    [InlineData(CefrLevel.A1)]
    [InlineData(CefrLevel.A2)]
    [InlineData(CefrLevel.B1)]
    [InlineData(CefrLevel.B2)]
    [InlineData(CefrLevel.C1)]
    [InlineData(CefrLevel.C2)]
    public void ToScore_then_FromScore_round_trips_to_same_level(CefrLevel level)
    {
        CefrLevelExtensions.FromScore(level.ToScore()).Should().Be(level);
    }

    [Theory]
    [InlineData(0, CefrLevel.A1)]
    [InlineData(43.4, CefrLevel.A2)]
    [InlineData(59.9, CefrLevel.B1)]
    [InlineData(100, CefrLevel.C2)]
    public void FromScore_maps_score_bands_to_levels(double score, CefrLevel expected)
    {
        CefrLevelExtensions.FromScore(score).Should().Be(expected);
    }
}
