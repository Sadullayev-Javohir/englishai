using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Subscription;

public class SpeakingMinuteAllowanceTests
{
    [Fact]
    public void Free_and_premium_get_different_daily_budgets()
    {
        SpeakingMinuteAllowance.DailyLimit(isPremium: false).Should().Be(SpeakingMinuteAllowance.FreeDailyMinutes);
        SpeakingMinuteAllowance.DailyLimit(isPremium: true).Should().Be(SpeakingMinuteAllowance.PremiumDailyMinutes);
    }

    [Fact]
    public void A_fresh_day_allows_speaking_and_reports_the_whole_budget()
    {
        var decision = SpeakingMinuteAllowance.Evaluate(isPremium: false, usedMinutes: 0);

        decision.IsAllowed.Should().BeTrue();
        decision.RemainingMinutes.Should().Be(SpeakingMinuteAllowance.FreeDailyMinutes);
    }

    [Fact]
    public void Partial_use_leaves_the_remainder()
    {
        var decision = SpeakingMinuteAllowance.Evaluate(isPremium: false, usedMinutes: 3.25);

        decision.IsAllowed.Should().BeTrue();
        decision.UsedMinutes.Should().Be(3.25);
        decision.RemainingMinutes.Should().Be(SpeakingMinuteAllowance.FreeDailyMinutes - 3.25);
    }

    [Fact]
    public void Reaching_the_budget_stops_further_speaking()
    {
        SpeakingMinuteAllowance.Evaluate(isPremium: false, usedMinutes: SpeakingMinuteAllowance.FreeDailyMinutes)
            .IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Overshooting_never_reports_negative_remaining()
    {
        // One utterance is allowed to tip the learner past the limit rather than throwing away
        // speech they have already produced, so the stored total can exceed the budget.
        var decision = SpeakingMinuteAllowance.Evaluate(isPremium: false, usedMinutes: 9);

        decision.IsAllowed.Should().BeFalse();
        decision.RemainingMinutes.Should().Be(0);
    }

    [Fact]
    public void A_premium_learner_keeps_going_where_a_free_one_stops()
    {
        var used = SpeakingMinuteAllowance.FreeDailyMinutes + 1;

        SpeakingMinuteAllowance.Evaluate(isPremium: false, used).IsAllowed.Should().BeFalse();
        SpeakingMinuteAllowance.Evaluate(isPremium: true, used).IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void A_corrupted_negative_total_is_treated_as_no_use()
    {
        var decision = SpeakingMinuteAllowance.Evaluate(isPremium: false, usedMinutes: -5);

        decision.UsedMinutes.Should().Be(0);
        decision.RemainingMinutes.Should().Be(SpeakingMinuteAllowance.FreeDailyMinutes);
    }
}
