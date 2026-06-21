using Application.Subscription.Dtos;
using MediatR;

namespace Application.Subscription.ConfirmPayment;

/// <summary>
/// Confirms a completed payment (PROJECT-SPEC H.3), invoked from the provider's webhook
/// after the gateway adapter has verified the callback's authenticity. Completes the
/// payment and activates Premium for the purchased plan. Idempotent: a repeat callback for
/// an already-completed payment returns the current subscription without double-activating.
/// </summary>
public sealed record ConfirmPaymentCommand(string TransactionId) : IRequest<SubscriptionDto>;
