using Domain.Common;
using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Subscription;

/// <summary>
/// Unit tests for the subscription lifecycle (PROJECT-SPEC Qism H). Time is always passed
/// explicitly - never DateTime.Now (docs/development-guide.md 17.2).
/// </summary>
public class SubscriptionTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private static Domain.Subscription.Subscription Free() =>
        Domain.Subscription.Subscription.CreateFree(Learner, Now);

    [Fact]
    public void New_subscription_is_free_and_not_premium()
    {
        var sub = Free();

        sub.Status.Should().Be(SubscriptionStatus.Free);
        sub.IsPremiumActive(Now).Should().BeFalse();
        sub.ExpiresAt.Should().BeNull();
        sub.DaysUntilExpiry(Now).Should().BeNull();
    }

    [Fact]
    public void Activating_monthly_sets_premium_for_thirty_days()
    {
        var sub = Free();

        sub.Activate(SubscriptionPlan.Monthly, Now);

        sub.Status.Should().Be(SubscriptionStatus.Premium);
        sub.Plan.Should().Be(SubscriptionPlan.Monthly);
        sub.ExpiresAt.Should().Be(Now.AddDays(SubscriptionPricing.MonthlyDurationDays));
        sub.IsPremiumActive(Now).Should().BeTrue();
        sub.DaysUntilExpiry(Now).Should().Be(SubscriptionPricing.MonthlyDurationDays);
    }

    [Fact]
    public void Activating_again_while_active_stacks_the_remaining_time()
    {
        var sub = Free();
        sub.Activate(SubscriptionPlan.Monthly, Now);

        // Renew 10 days in - the new month stacks onto the remaining 20 days.
        var renewAt = Now.AddDays(10);
        sub.Activate(SubscriptionPlan.Monthly, renewAt);

        sub.ExpiresAt.Should().Be(Now.AddDays(SubscriptionPricing.MonthlyDurationDays * 2));
    }

    [Fact]
    public void Reactivating_after_expiry_starts_from_now()
    {
        var sub = Free();
        sub.Activate(SubscriptionPlan.Monthly, Now);

        var reactivateAt = Now.AddDays(40); // already lapsed
        sub.Activate(SubscriptionPlan.Monthly, reactivateAt);

        sub.ExpiresAt.Should().Be(reactivateAt.AddDays(SubscriptionPricing.MonthlyDurationDays));
    }

    [Fact]
    public void Cancelling_keeps_access_until_expiry()
    {
        var sub = Free();
        sub.Activate(SubscriptionPlan.Monthly, Now);

        sub.Cancel(Now);

        sub.Status.Should().Be(SubscriptionStatus.Cancelled);
        sub.IsPremiumActive(Now).Should().BeTrue();
        sub.IsPremiumActive(Now.AddDays(31)).Should().BeFalse();
    }

    [Fact]
    public void Cancelling_a_free_subscription_is_rejected()
    {
        var act = () => Free().Cancel(Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expire_if_elapsed_transitions_premium_to_expired_after_the_period()
    {
        var sub = Free();
        sub.Activate(SubscriptionPlan.Monthly, Now);

        sub.ExpireIfElapsed(Now.AddDays(15)).Should().BeFalse();
        sub.Status.Should().Be(SubscriptionStatus.Premium);

        sub.ExpireIfElapsed(Now.AddDays(31)).Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Expired);
    }

    [Fact]
    public void Expire_if_elapsed_also_expires_a_cancelled_subscription()
    {
        var sub = Free();
        sub.Activate(SubscriptionPlan.Yearly, Now);
        sub.Cancel(Now);

        sub.ExpireIfElapsed(Now.AddDays(SubscriptionPricing.YearlyDurationDays + 1)).Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Expired);
    }
}
