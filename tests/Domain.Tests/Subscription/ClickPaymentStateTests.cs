using Domain.Subscription;
using FluentAssertions;

namespace Domain.Tests.Subscription;

public sealed class ClickPaymentStateTests
{
    [Fact]
    public void Prepare_is_idempotent_for_the_same_click_transaction()
    {
        var payment = Payment.Start(
            Guid.NewGuid(), SubscriptionPlan.Monthly, PaymentProvider.Click,
            "click-order", DateTimeOffset.UtcNow);

        payment.Prepare(123, 456);
        var prepareId = payment.ProviderPrepareId;
        payment.Prepare(123, 456);

        payment.ProviderTransactionId.Should().Be(123);
        payment.ProviderPaymentId.Should().Be(456);
        payment.ProviderPrepareId.Should().Be(prepareId);
        prepareId.Should().BePositive();
    }
}
