using Domain.Subscription;

namespace Application.Subscription.Ports;

/// <summary>
/// Persistence port for <see cref="Payment"/> records. Lookups are keyed by the
/// provider transaction id so confirmation callbacks are idempotent.
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken);

    Task<Payment?> GetByProviderTransactionIdAsync(long providerTransactionId, CancellationToken cancellationToken);

    Task<Payment?> GetByProviderPrepareIdAsync(int providerPrepareId, CancellationToken cancellationToken);

    Task SaveAsync(Payment payment, CancellationToken cancellationToken);
}
