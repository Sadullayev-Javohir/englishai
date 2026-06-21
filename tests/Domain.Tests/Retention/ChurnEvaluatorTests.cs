using Domain.Retention;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Retention;

/// <summary>
/// Unit tests for churn-risk scoring (PROJECT-SPEC I.1). The evaluator is pure: every input
/// is supplied explicitly via a snapshot - no clock or storage.
/// </summary>
public class ChurnEvaluatorTests
{
    private static ChurnInputs HealthyActive => new()
    {
        DaysSinceLastActivity = 0,
        OnboardingCompleted = true
    };

    [Fact]
    public void A_healthy_active_learner_has_no_signals()
    {
        var assessment = ChurnEvaluator.Evaluate(HealthyActive);

        assessment.Signals.Should().BeEmpty();
        assessment.OverallRisk.Should().Be(ChurnRiskLevel.None);
        assessment.IsAtRisk.Should().BeFalse();
    }

    [Fact]
    public void Unfinished_onboarding_is_very_high_risk()
    {
        var assessment = ChurnEvaluator.Evaluate(HealthyActive with { OnboardingCompleted = false });

        assessment.Signals.Should().ContainSingle()
            .Which.Type.Should().Be(ChurnSignalType.OnboardingIncomplete);
        assessment.OverallRisk.Should().Be(ChurnRiskLevel.VeryHigh);
    }

    [Fact]
    public void Seven_days_without_activity_is_high_risk()
    {
        var assessment = ChurnEvaluator.Evaluate(
            HealthyActive with { DaysSinceLastActivity = ChurnEvaluator.NoActivityDays });

        assessment.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.NoActivitySevenDays);
        assessment.OverallRisk.Should().Be(ChurnRiskLevel.High);
    }

    [Fact]
    public void Six_days_without_activity_does_not_fire_the_no_activity_signal()
    {
        var assessment = ChurnEvaluator.Evaluate(
            HealthyActive with { DaysSinceLastActivity = ChurnEvaluator.NoActivityDays - 1 });

        assessment.Signals.Select(s => s.Type).Should().NotContain(ChurnSignalType.NoActivitySevenDays);
    }

    [Fact]
    public void Unresolved_speaking_frustration_is_high_risk()
    {
        var assessment = ChurnEvaluator.Evaluate(
            HealthyActive with { UnresolvedSpeakingFrustration = true });

        assessment.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.SpeakingFrustration);
        assessment.OverallRisk.Should().Be(ChurnRiskLevel.High);
    }

    [Fact]
    public void A_lapsed_streak_is_medium_risk()
    {
        var assessment = ChurnEvaluator.Evaluate(HealthyActive with { StreakLapsed = true });

        assessment.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.StreakBrokenTwoDays);
        assessment.OverallRisk.Should().Be(ChurnRiskLevel.Medium);
    }

    [Fact]
    public void Rising_srs_failures_is_medium_risk()
    {
        var assessment = ChurnEvaluator.Evaluate(HealthyActive with { RisingSrsFailures = true });

        assessment.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.RisingSrsFailures);
        assessment.OverallRisk.Should().Be(ChurnRiskLevel.Medium);
    }

    [Fact]
    public void Premium_expiring_soon_with_low_activity_is_high_risk()
    {
        var assessment = ChurnEvaluator.Evaluate(HealthyActive with
        {
            DaysUntilPremiumExpiry = ChurnEvaluator.PremiumExpiryWindowDays,
            DaysSinceLastActivity = ChurnEvaluator.LowActivityDays
        });

        assessment.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.PremiumLapsingWithLowActivity);
    }

    [Fact]
    public void Premium_expiring_soon_but_still_active_does_not_fire()
    {
        var assessment = ChurnEvaluator.Evaluate(HealthyActive with
        {
            DaysUntilPremiumExpiry = ChurnEvaluator.PremiumExpiryWindowDays,
            DaysSinceLastActivity = 0
        });

        assessment.Signals.Select(s => s.Type).Should().NotContain(ChurnSignalType.PremiumLapsingWithLowActivity);
    }

    [Fact]
    public void Overall_risk_is_the_maximum_of_all_fired_signals()
    {
        // A lapsed streak (Medium) plus seven-day inactivity (High) → overall High.
        var assessment = ChurnEvaluator.Evaluate(HealthyActive with
        {
            StreakLapsed = true,
            DaysSinceLastActivity = ChurnEvaluator.NoActivityDays
        });

        assessment.Signals.Should().HaveCount(2);
        assessment.OverallRisk.Should().Be(ChurnRiskLevel.High);
    }
}
