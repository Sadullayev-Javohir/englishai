using Domain.Subscription;

namespace Application.Subscription.Ports;

/// <summary>
/// Payment provider port (PROJECT-SPEC H.3). Implemented by the dev <c>LocalPaymentGateway</c>
/// and, in production, by Click/Payme adapters - selecting a provider never touches the
/// Application layer (docs/development-guide.md rule 10, ports/adapters). The adapter initiates a checkout
/// and returns where to send the user; the provider later confirms via a webhook that
/// drives <c>ConfirmPaymentCommand</c>.
/// </summary>
public interface IPaymentGateway
{
    PaymentProvider Provider { get; }

    Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request, CancellationToken cancellationToken);
}

public interface IPaymentGatewayResolver
{
    IReadOnlyCollection<PaymentProvider> AvailableProviders { get; }

    IPaymentGateway Resolve(PaymentProvider provider);
}

/// <summary>Inputs for starting a checkout.</summary>
public sealed record PaymentInitiationRequest(
    Guid LearnerId,
    SubscriptionPlan Plan,
    int AmountUzs,
    string ReturnUrl,
    string WebhookUrl);

/// <summary>
/// Result of starting a checkout: the provider transaction id (idempotency key for the
/// confirmation callback) and the URL the user is sent to in order to pay.
/// </summary>
public sealed record PaymentInitiationResult(string TransactionId, string CheckoutUrl);
