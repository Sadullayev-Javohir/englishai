using System.Collections.Concurrent;
using Application.Subscription.Ports;
using Domain.Subscription;

namespace Infrastructure.Subscription;

/// <summary>
/// Dev/test <see cref="IPaymentRepository"/> keyed by transaction id. Used when no database
/// is configured; EF Core is the durable production path.
/// </summary>
public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<string, Payment> _byTransactionId = new();

    public Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken)
    {
        _byTransactionId.TryGetValue(transactionId, out var payment);
        return Task.FromResult(payment);
    }

    public Task<Payment?> GetByProviderTransactionIdAsync(
        long providerTransactionId, CancellationToken cancellationToken) =>
        Task.FromResult(_byTransactionId.Values.FirstOrDefault(
            payment => payment.ProviderTransactionId == providerTransactionId));

    public Task<Payment?> GetByProviderPrepareIdAsync(
        int providerPrepareId, CancellationToken cancellationToken) =>
        Task.FromResult(_byTransactionId.Values.FirstOrDefault(
            payment => payment.ProviderPrepareId == providerPrepareId));

    public Task SaveAsync(Payment payment, CancellationToken cancellationToken)
    {
        _byTransactionId[payment.TransactionId] = payment;
        return Task.CompletedTask;
    }
}
