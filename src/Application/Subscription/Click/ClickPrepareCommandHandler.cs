using Application.Subscription.Ports;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.Click;

public sealed class ClickPrepareCommandHandler
    : IRequestHandler<ClickPrepareCommand, ClickPrepareResult>
{
    private readonly IPaymentRepository _payments;

    public ClickPrepareCommandHandler(IPaymentRepository payments) => _payments = payments;

    public async Task<ClickPrepareResult> Handle(
        ClickPrepareCommand request, CancellationToken cancellationToken)
    {
        if (request.Action != 0)
            throw new ClickPaymentException(-3, "Action not found");
        if (request.Error < 0)
            throw new ClickPaymentException(-8, "Error in request from click");

        var payment = await _payments.GetByTransactionIdAsync(
            request.MerchantTransactionId, cancellationToken);
        if (payment is null || payment.Provider != PaymentProvider.Click)
            throw new ClickPaymentException(-5, "User does not exist");
        if (payment.Status == PaymentStatus.Failed)
            throw new ClickPaymentException(-9, "Transaction cancelled");
        if (request.Amount != payment.AmountUzs)
            throw new ClickPaymentException(-2, "Incorrect parameter amount");

        try
        {
            payment.Prepare(request.ClickTransactionId, request.ClickPaymentId);
        }
        catch (Domain.Common.DomainException)
        {
            throw new ClickPaymentException(-8, "Error in request from click");
        }

        await _payments.SaveAsync(payment, cancellationToken);
        return new ClickPrepareResult(payment.ProviderPrepareId!.Value);
    }
}
