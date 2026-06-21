using Application.Subscription.ConfirmPayment;
using Application.Subscription.Ports;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.Click;

public sealed class ClickCompleteCommandHandler
    : IRequestHandler<ClickCompleteCommand, ClickCompleteResult>
{
    private readonly IPaymentRepository _payments;
    private readonly ConfirmPaymentCommandHandler _confirmPayment;
    private readonly TimeProvider _clock;

    public ClickCompleteCommandHandler(
        IPaymentRepository payments,
        ConfirmPaymentCommandHandler confirmPayment,
        TimeProvider clock)
    {
        _payments = payments;
        _confirmPayment = confirmPayment;
        _clock = clock;
    }

    public async Task<ClickCompleteResult> Handle(
        ClickCompleteCommand request, CancellationToken cancellationToken)
    {
        if (request.Action != 1)
            throw new ClickPaymentException(-3, "Action not found");

        var payment = await _payments.GetByProviderPrepareIdAsync(
            request.MerchantPrepareId, cancellationToken);
        if (payment is null || payment.Provider != PaymentProvider.Click)
            throw new ClickPaymentException(-6, "Transaction does not exist");
        if (!string.Equals(
                payment.TransactionId, request.MerchantTransactionId, StringComparison.Ordinal) ||
            payment.ProviderTransactionId != request.ClickTransactionId ||
            payment.ProviderPaymentId != request.ClickPaymentId)
            throw new ClickPaymentException(-6, "Transaction does not exist");
        if (request.Amount != payment.AmountUzs)
            throw new ClickPaymentException(-2, "Incorrect parameter amount");
        if (payment.Status == PaymentStatus.Failed)
            throw new ClickPaymentException(-9, "Transaction cancelled");

        if (request.Error < 0)
        {
            payment.Fail(_clock.GetUtcNow());
            await _payments.SaveAsync(payment, cancellationToken);
            throw new ClickPaymentException(-9, "Transaction cancelled");
        }

        if (payment.Status == PaymentStatus.Completed)
            return new ClickCompleteResult(payment.ProviderPrepareId!.Value);

        await _confirmPayment.Handle(new ConfirmPaymentCommand(payment.TransactionId), cancellationToken);
        return new ClickCompleteResult(payment.ProviderPrepareId!.Value);
    }
}
