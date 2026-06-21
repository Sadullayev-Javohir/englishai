using Domain.Retention;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Retention;

/// <summary>
/// Unit tests for D1/D7/D30 cohort retention math (PROJECT-SPEC I.4). "asOf" is always
/// passed explicitly - never DateTime.Now (docs/development-guide.md 17.2).
/// </summary>
public class RetentionMetricsCalculatorTests
{
    private static readonly DateOnly AsOf = new(2026, 6, 22);

    private static LearnerActivityWindow Window(DateOnly registered, params DateOnly[] active) =>
        new(registered, active);

    [Fact]
    public void Empty_cohort_yields_zero_rates()
    {
        var metrics = RetentionMetricsCalculator.Compute(Array.Empty<LearnerActivityWindow>(), AsOf);

        metrics.CohortSize.Should().Be(0);
        metrics.D1.Rate.Should().Be(0);
        metrics.D7.EligibleLearners.Should().Be(0);
    }

    [Fact]
    public void A_learner_active_on_day_one_counts_as_retained()
    {
        var registered = AsOf.AddDays(-10);
        var window = Window(registered, registered, registered.AddDays(1)); // active on D1
        var metrics = RetentionMetricsCalculator.Compute(new[] { window }, AsOf);

        metrics.D1.EligibleLearners.Should().Be(1);
        metrics.D1.RetainedLearners.Should().Be(1);
        metrics.D1.Rate.Should().Be(1.0);
    }

    [Fact]
    public void A_learner_who_did_not_return_on_day_one_is_eligible_but_not_retained()
    {
        var registered = AsOf.AddDays(-10);
        var window = Window(registered, registered); // active only on registration day
        var metrics = RetentionMetricsCalculator.Compute(new[] { window }, AsOf);

        metrics.D1.EligibleLearners.Should().Be(1);
        metrics.D1.RetainedLearners.Should().Be(0);
        metrics.D1.Rate.Should().Be(0);
    }

    [Fact]
    public void A_learner_too_new_for_a_horizon_is_excluded_from_its_denominator()
    {
        // Registered 3 days ago: eligible for D1, not yet for D7 or D30.
        var registered = AsOf.AddDays(-3);
        var window = Window(registered, registered, registered.AddDays(1));
        var metrics = RetentionMetricsCalculator.Compute(new[] { window }, AsOf);

        metrics.D1.EligibleLearners.Should().Be(1);
        metrics.D7.EligibleLearners.Should().Be(0);
        metrics.D30.EligibleLearners.Should().Be(0);
    }

    [Fact]
    public void Day7_and_day30_count_activity_on_the_exact_horizon_day()
    {
        var registered = AsOf.AddDays(-40);
        var window = Window(
            registered,
            registered.AddDays(7),   // D7 hit
            registered.AddDays(30)); // D30 hit
        var metrics = RetentionMetricsCalculator.Compute(new[] { window }, AsOf);

        metrics.D7.RetainedLearners.Should().Be(1);
        metrics.D30.RetainedLearners.Should().Be(1);
        metrics.D1.RetainedLearners.Should().Be(0);
    }

    [Fact]
    public void Rate_is_the_fraction_of_eligible_learners_retained()
    {
        var registered = AsOf.AddDays(-10);
        var retained = Window(registered, registered, registered.AddDays(1));
        var lapsed = Window(registered, registered);
        var metrics = RetentionMetricsCalculator.Compute(new[] { retained, lapsed }, AsOf);

        metrics.CohortSize.Should().Be(2);
        metrics.D1.EligibleLearners.Should().Be(2);
        metrics.D1.RetainedLearners.Should().Be(1);
        metrics.D1.Rate.Should().Be(0.5);
    }
}
