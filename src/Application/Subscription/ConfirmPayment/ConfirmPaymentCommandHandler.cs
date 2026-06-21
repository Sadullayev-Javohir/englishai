using Application.Analytics.Ports;
using Application.Common;
using Application.Subscription.Dtos;
using Application.Subscription.Ports;
using Domain.Analytics;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.ConfirmPayment;

public sealed class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, SubscriptionDto>
{
    private readonly IPaymentRepository _payments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;

    public ConfirmPaymentCommandHandler(
        IPaymentRepository payments,
        ISubscriptionRepository subscriptions,
        IProductEventStore productEvents,
        TimeProvider clock)
    {
        _payments = payments;
        _subscriptions = subscriptions;
        _productEvents = productEvents;
        _clock = clock;
    }

    public async Task<SubscriptionDto> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();

        var payment = await _payments.GetByTransactionIdAsync(request.TransactionId, cancellationToken)
                      ?? throw new NotFoundException("Payment", request.TransactionId);

        var alreadyCompleted = payment.Status == PaymentStatus.Completed;

        payment.Complete(now);
        await _payments.SaveAsync(payment, cancellationToken);

        var subscription = await _subscriptions.GetByLearnerIdAsync(payment.LearnerId, cancellationToken)
                           ?? Domain.Subscription.Subscription.CreateFree(payment.LearnerId, now);

        // Idempotency: only activate on the first confirmation so a duplicate webhook does
        // not stack extra paid time.
        if (!alreadyCompleted)
        {
            subscription.Activate(payment.Plan, now);
            await _subscriptions.SaveAsync(subscription, cancellationToken);
            await _productEvents.AppendOnceAsync(
                payment.LearnerId,
                ProductEventType.PaymentCompleted,
                now,
                source: payment.Plan.ToString(),
                cancellationToken);
        }

        return SubscriptionDto.From(subscription, now);
    }
}
