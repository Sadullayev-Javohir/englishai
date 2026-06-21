using FluentAssertions;
using Infrastructure.Ai;
using Infrastructure.Speaking;
using Xunit;

namespace Integration.Tests.Ai;

/// <summary>
/// Pins the deliberate asymmetry between the two pricing guards: zero LLM rates are survivable
/// (the backend really is free today) and only warn, whereas a zero Voice Live rate always hides
/// real money and must stop the process.
/// </summary>
public sealed class AiPricingGuardTests
{
    [Fact]
    public void Zero_token_rates_with_pricing_configured_are_allowed_but_flagged()
    {
        var options = new AiAdmissionOptions { PricingConfigured = true };

        options.Invoking(value => value.ValidatePricing()).Should().NotThrow();
        options.HasZeroPricing.Should().BeTrue();
    }

    [Fact]
    public void Real_rates_with_pricing_switched_off_are_rejected()
    {
        var options = new AiAdmissionOptions
        {
            PricingConfigured = false,
            EstimatedInputCostPerMillionTokensUsd = 2,
        };

        options.Invoking(value => value.ValidatePricing())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*would ignore real LLM spend*");
    }

    [Fact]
    public void A_non_positive_daily_budget_is_rejected()
    {
        var options = new AiAdmissionOptions { PricingConfigured = true, DailyBudgetUsd = 0 };

        options.Invoking(value => value.ValidatePricing())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*DailyBudgetUsd*");
    }

    [Fact]
    public void Configured_rates_are_not_flagged()
    {
        var options = new AiAdmissionOptions
        {
            PricingConfigured = true,
            EstimatedInputCostPerMillionTokensUsd = 0.15,
            EstimatedOutputCostPerMillionTokensUsd = 0.60,
        };

        options.HasZeroPricing.Should().BeFalse();
        options.Invoking(value => value.ValidatePricing()).Should().NotThrow();
    }

    [Fact]
    public void Voice_live_pricing_declared_without_a_rate_is_rejected()
    {
        var options = new AzureVoiceLiveOptions { PricingConfigured = true };

        options.Invoking(value => value.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*CostPerAudioHourUsd is not set*");
    }

    [Fact]
    public void Voice_live_falls_back_to_a_deliberately_high_rate_when_unpriced()
    {
        var options = new AzureVoiceLiveOptions();

        options.Invoking(value => value.Validate()).Should().NotThrow();
        options.EffectiveCostPerAudioHourUsd.Should().Be(AzureVoiceLiveOptions.DefaultCostPerAudioHourUsd);
    }

    [Theory]
    [InlineData(10, 0, 30, "ReservationMinutes")]
    [InlineData(10, 11, 30, "ReservationMinutes")]
    [InlineData(10, 2, 5, "DailyMinutesPerLearner")]
    [InlineData(0, 2, 30, "MaxSessionMinutes")]
    public void Voice_live_caps_must_be_internally_consistent(
        int maxSessionMinutes,
        int reservationMinutes,
        int dailyMinutes,
        string expected)
    {
        var options = new AzureVoiceLiveOptions
        {
            CostPerAudioHourUsd = 30,
            MaxSessionMinutes = maxSessionMinutes,
            ReservationMinutes = reservationMinutes,
            DailyMinutesPerLearner = dailyMinutes,
        };

        options.Invoking(value => value.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage($"*{expected}*");
    }
}
