using Application.Analytics.Ports;
using Application.Common;
using Application.Gamification.Ports;
using Application.Subscription.Dtos;
using Application.Subscription.Ports;
using Domain.Analytics;
using Domain.Common;
using Domain.Gamification;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.StartSubscription;

public sealed class StartSubscriptionCommandHandler
    : IRequestHandler<StartSubscriptionCommand, StartSubscriptionResultDto>
{
    private readonly IPaymentGatewayResolver _gateways;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentAvailability _availability;
    private readonly IProductEventStore _productEvents;
    private readonly IDiscountRedemptionRepository _redemptions;
    private readonly TimeProvider _clock;

    public StartSubscriptionCommandHandler(
        IPaymentGatewayResolver gateways,
        IPaymentRepository payments,
        IPaymentAvailability availability,
        IProductEventStore productEvents,
        IDiscountRedemptionRepository redemptions,
        TimeProvider clock)
    {
        _gateways = gateways;
        _payments = payments;
        _availability = availability;
        _productEvents = productEvents;
        _redemptions = redemptions;
        _clock = clock;
    }

    public async Task<StartSubscriptionResultDto> Handle(
        StartSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        await _productEvents.AppendOnceAsync(
            request.LearnerId,
            ProductEventType.UpgradeClicked,
            now,
            source: request.Plan.ToString(),
            cancellationToken);

        // Until a real Click/Payme gateway is wired up, only the free plan is offered. Refuse the
        // upgrade here so Premium is never granted without a real payment (the dev gateway would
        // otherwise auto-confirm). The flag flips on once a paid gateway is configured.
        if (!_availability.PaymentsEnabled)
            throw new PaymentsUnavailableException();
        var amount = SubscriptionPricing.PriceUzs(request.Plan);

        // Leaderboard/points feature: an optional coupon (bought with SpendableCoins via
        // RedeemDiscountCommand) reduces the charged amount. Ownership + expiry are re-checked
        // here rather than trusted from the client, and it is consumed immediately (same risk
        // model as the Payment created just below: both can be orphaned by a failed checkout,
        // consistent with how this handler already treats payment initiation as best-effort).
        DiscountRedemption? redemption = null;
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            redemption = await _redemptions.GetByCodeAsync(request.DiscountCode, cancellationToken);
            if (redemption is null || redemption.LearnerId != request.LearnerId || !redemption.IsUsable(now))
                throw new DomainException("This discount code is not valid for this account.");

            amount = DiscountCatalog.CalculateDiscountedAmount(amount, redemption.DiscountPercent);
        }

        var gateway = _gateways.Resolve(request.Provider);
        var webhookUrl = $"{request.WebhookBaseUrl.TrimEnd('/')}/api/subscription/payments/webhook/{request.Provider.ToString().ToLowerInvariant()}";
        var initiation = await gateway.InitiatePaymentAsync(
            new PaymentInitiationRequest(
                request.LearnerId,
                request.Plan,
                amount,
                request.ReturnUrl,
                webhookUrl),
            cancellationToken);

        var payment = Payment.Start(
            request.LearnerId, request.Plan, gateway.Provider, initiation.TransactionId, now,
            amountOverrideUzs: amount);
        await _payments.SaveAsync(payment, cancellationToken);

        if (redemption is not null)
        {
            redemption.MarkUsed(now);
            await _redemptions.SaveAsync(redemption, cancellationToken);
        }

        return new StartSubscriptionResultDto(
            initiation.TransactionId,
            initiation.CheckoutUrl,
            amount,
            gateway.Provider,
            request.Plan);
    }
}
