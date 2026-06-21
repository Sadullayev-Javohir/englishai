using Domain.Common;
using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Subscription;

public class PaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private static Payment Start(SubscriptionPlan plan = SubscriptionPlan.Monthly) =>
        Payment.Start(Learner, plan, PaymentProvider.Local, "local-tx-1", Now);

    [Fact]
    public void Starting_a_payment_captures_the_plan_price_and_is_pending()
    {
        var payment = Start();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.AmountUzs.Should().Be(SubscriptionPricing.MonthlyPriceUzs);
        payment.Provider.Should().Be(PaymentProvider.Local);
    }

    [Fact]
    public void Completing_marks_completed_and_is_idempotent()
    {
        var payment = Start();

        payment.Complete(Now);
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.CompletedAt.Should().Be(Now);

        // Repeat callback: no throw, still completed.
        payment.Complete(Now.AddMinutes(5));
        payment.Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void A_failed_payment_cannot_be_completed()
    {
        var payment = Start();
        payment.Fail(Now);

        var act = () => payment.Complete(Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_transaction_id_is_rejected()
    {
        var act = () => Payment.Start(Learner, SubscriptionPlan.Monthly, PaymentProvider.Local, "", Now);

        act.Should().Throw<DomainException>();
    }
}
