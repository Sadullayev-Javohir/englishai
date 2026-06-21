using Domain.Common;
using Domain.Gamification;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Gamification;

/// <summary>Unit tests for the durable learner points and unit-by-unit energy clock.</summary>
public class LearnerPointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    [Fact]
    public void Earn_credits_both_lifetime_xp_and_spendable_coins()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        points.Earn(10, Now);
        points.LifetimeXp.Should().Be(10);
        points.SpendableCoins.Should().Be(10);
    }

    [Fact]
    public void Earn_rejects_a_non_positive_amount()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        var act = () => points.Earn(0, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Redeem_debits_only_spendable_coins_leaving_lifetime_xp_untouched()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        points.Earn(1000, Now);
        points.Redeem(500, Now);
        points.SpendableCoins.Should().Be(500);
        points.LifetimeXp.Should().Be(1000);
    }

    [Fact]
    public void Redeem_throws_when_coins_are_insufficient()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        points.Earn(100, Now);
        var act = () => points.Redeem(500, Now);
        act.Should().Throw<DomainException>();
        points.SpendableCoins.Should().Be(100);
    }

    [Fact]
    public void Energy_does_not_refill_before_36_minutes_then_adds_one_unit()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        points.TryConsumeEnergy(Now).Should().BeTrue();

        points.RefreshEnergy(Now.AddMinutes(35)).Should().BeFalse();
        points.Energy.Should().Be(4);
        points.RefreshEnergy(Now.AddMinutes(36)).Should().BeTrue();
        points.Energy.Should().Be(5);
        points.NextEnergyRefillAt.Should().BeNull();
        points.FullEnergyRefillAt.Should().BeNull();
    }

    [Fact]
    public void Empty_energy_refills_to_five_after_three_hours_and_stops_the_clock()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        for (var unit = 0; unit < EnergyPolicy.MaximumEnergy; unit++)
            points.TryConsumeEnergy(Now);

        points.Energy.Should().Be(0);
        points.FullEnergyRefillAt.Should().Be(Now.AddHours(3));

        points.RefreshEnergy(Now.AddHours(3)).Should().BeTrue();
        points.Energy.Should().Be(EnergyPolicy.MaximumEnergy);
        points.NextEnergyRefillAt.Should().BeNull();
        points.FullEnergyRefillAt.Should().BeNull();
    }

    [Fact]
    public void Spending_from_full_resets_the_refill_anchor_to_the_spend_time()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        var spentAt = Now.AddHours(9);

        points.TryConsumeEnergy(spentAt).Should().BeTrue();

        points.NextEnergyRefillAt.Should().Be(spentAt.AddMinutes(36));
        points.FullEnergyRefillAt.Should().Be(spentAt.AddMinutes(36));
    }

    [Fact]
    public void Energy_cannot_drop_below_zero()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        for (var unit = 0; unit < EnergyPolicy.MaximumEnergy; unit++)
            points.TryConsumeEnergy(Now);

        points.TryConsumeEnergy(Now.AddMinutes(2)).Should().BeFalse();
        points.Energy.Should().Be(0);
    }

    [Fact]
    public void Streak_milestone_is_awarded_once_when_first_crossed()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        points.TryAwardStreakMilestone(7, Now).Should().Be(50);
        points.HighestStreakMilestoneReached.Should().Be(7);
    }

    [Fact]
    public void Streak_milestone_does_not_re_award_the_same_threshold()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        points.TryAwardStreakMilestone(7, Now);
        points.TryAwardStreakMilestone(7, Now).Should().BeNull();
    }

    [Fact]
    public void Streak_milestone_awards_the_highest_newly_crossed_threshold()
    {
        var points = LearnerPoints.CreateNew(Learner, Now);
        points.TryAwardStreakMilestone(30, Now).Should().Be(200);
        points.HighestStreakMilestoneReached.Should().Be(30);
    }
}
