using Application.Subscription.Click;
using Application.Subscription.Ports;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Subscription;

public sealed class ClickPaymentHandlerTests
{
    [Fact]
    public async Task Prepare_links_click_transaction_and_returns_stable_prepare_id()
    {
        var payments = Substitute.For<IPaymentRepository>();
        var payment = Payment.Start(
            Guid.NewGuid(), SubscriptionPlan.Monthly, PaymentProvider.Click,
            "click-order", DateTimeOffset.UtcNow);
        payments.GetByTransactionIdAsync(payment.TransactionId, Arg.Any<CancellationToken>())
            .Returns(payment);
        var handler = new ClickPrepareCommandHandler(payments);

        var result = await handler.Handle(new ClickPrepareCommand(
            10, 20, payment.TransactionId, payment.AmountUzs, 0, 0, "Success"), CancellationToken.None);

        result.MerchantPrepareId.Should().BePositive();
        payment.ProviderTransactionId.Should().Be(10);
        payment.ProviderPaymentId.Should().Be(20);
    }

    [Fact]
    public async Task Prepare_rejects_an_incorrect_amount()
    {
        var payments = Substitute.For<IPaymentRepository>();
        var payment = Payment.Start(
            Guid.NewGuid(), SubscriptionPlan.Monthly, PaymentProvider.Click,
            "click-order", DateTimeOffset.UtcNow);
        payments.GetByTransactionIdAsync(payment.TransactionId, Arg.Any<CancellationToken>())
            .Returns(payment);
        var handler = new ClickPrepareCommandHandler(payments);

        var act = () => handler.Handle(new ClickPrepareCommand(
            10, 20, payment.TransactionId, payment.AmountUzs + 1, 0, 0, "Success"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ClickPaymentException>();
        exception.Which.ErrorCode.Should().Be(-2);
    }
}
