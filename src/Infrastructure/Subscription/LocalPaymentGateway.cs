using Application.Subscription.Ports;
using Domain.Subscription;

namespace Infrastructure.Subscription;

/// <summary>
/// Dev/test payment gateway (PROJECT-SPEC H.3). It issues a deterministic transaction id
/// and a checkout URL that points back at the app's own confirmation endpoint, so the
/// full purchase → confirm → activate flow works end-to-end without a real provider. Real
/// Click/Payme adapters implement the same <see cref="IPaymentGateway"/> port and require
/// merchant credentials (docs/development-guide.md rules 10, 13).
/// </summary>
public sealed class LocalPaymentGateway : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Local;

    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request, CancellationToken cancellationToken)
    {
        var transactionId = $"local-{Guid.NewGuid():N}";

        // In production this would be the provider-hosted checkout page; locally we point at
        // our own webhook-style confirm endpoint so the flow can be exercised end-to-end.
        var checkoutUrl = $"/api/subscription/payments/mock-checkout?provider=local&transactionId={transactionId}&returnUrl={Uri.EscapeDataString(request.ReturnUrl)}";

        return Task.FromResult(new PaymentInitiationResult(transactionId, checkoutUrl));
    }
}
