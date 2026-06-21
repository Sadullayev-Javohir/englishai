using Domain.Assessment;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Assessment;

public class AbilityEstimatorTests
{
    private static AbilityResponse R(CefrLevel level, bool correct) =>
        new(level.ToScore(), correct);

    [Fact]
    public void No_responses_returns_zero()
    {
        AbilityEstimator.Estimate(Array.Empty<AbilityResponse>()).Should().Be(0);
    }

    [Fact]
    public void Consistent_correctness_up_to_a_level_estimates_near_that_level()
    {
        var responses = new[]
        {
            R(CefrLevel.A2, true), R(CefrLevel.A2, true), R(CefrLevel.B1, true),
            R(CefrLevel.B1, true), R(CefrLevel.B2, false), R(CefrLevel.B2, false),
        };

        var ability = AbilityEstimator.Estimate(responses);

        CefrLevelExtensions.FromScore(ability).Should().BeOneOf(CefrLevel.B1, CefrLevel.A2);
    }

    [Fact]
    public void One_lucky_correct_far_above_ability_barely_moves_the_estimate()
    {
        var grounded = new[]
        {
            R(CefrLevel.A2, true), R(CefrLevel.A2, true), R(CefrLevel.A2, true),
            R(CefrLevel.B1, false), R(CefrLevel.B1, false),
        };
        var withFluke = grounded.Append(R(CefrLevel.C2, true)).ToList();

        var baseAbility = AbilityEstimator.Estimate(grounded);
        var flukeAbility = AbilityEstimator.Estimate(withFluke);

        // The guessing floor explains the fluke, so the estimate moves only slightly
        // and certainly does not jump toward C2.
        (flukeAbility - baseAbility).Should().BeLessThan(CefrLevel.B1.ToScore() - CefrLevel.A2.ToScore());
        CefrLevelExtensions.FromScore(flukeAbility).Should().BeOneOf(CefrLevel.A2, CefrLevel.B1);
    }

    [Fact]
    public void All_wrong_estimates_at_the_floor()
    {
        var responses = new[] { R(CefrLevel.A2, false), R(CefrLevel.A1, false), R(CefrLevel.A2, false) };

        CefrLevelExtensions.FromScore(AbilityEstimator.Estimate(responses)).Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void Probability_increases_with_ability_and_never_drops_below_the_guess_floor()
    {
        var low = AbilityEstimator.ProbabilityCorrect(20, 70);
        var high = AbilityEstimator.ProbabilityCorrect(90, 70);

        low.Should().BeGreaterThanOrEqualTo(AbilityEstimator.GuessProbability);
        high.Should().BeGreaterThan(low);
        high.Should().BeLessThan(1.0);
    }
}
