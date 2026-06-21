using Application.Subscription.Dtos;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.StartSubscription;

/// <summary>
/// Begins a subscription purchase (PROJECT-SPEC H.3): records a pending payment and asks
/// the configured gateway to start a checkout. Premium is activated only once the provider
/// confirms the payment (<c>ConfirmPaymentCommand</c>). <paramref name="DiscountCode"/> is an
/// optional coupon code redeemed via the leaderboard/points feature (<c>RedeemDiscountCommand</c>)
/// that, if valid and owned by this learner, reduces the charged amount.
/// </summary>
public sealed record StartSubscriptionCommand(
    Guid LearnerId,
    SubscriptionPlan Plan,
    PaymentProvider Provider,
    string ReturnUrl,
    string WebhookBaseUrl,
    string? DiscountCode = null)
    : IRequest<StartSubscriptionResultDto>;
