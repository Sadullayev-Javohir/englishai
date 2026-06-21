using Application.Subscription.Ports;
using Domain.Subscription;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Subscription;

/// <summary>EF Core (PostgreSQL) <see cref="IPaymentRepository"/>.</summary>
public sealed class EfPaymentRepository : IPaymentRepository
{
    private readonly EnglishAiDbContext _db;

    public EfPaymentRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken) =>
        _db.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);

    public Task<Payment?> GetByProviderTransactionIdAsync(
        long providerTransactionId, CancellationToken cancellationToken) =>
        _db.Payments.FirstOrDefaultAsync(
            p => p.ProviderTransactionId == providerTransactionId, cancellationToken);

    public Task<Payment?> GetByProviderPrepareIdAsync(
        int providerPrepareId, CancellationToken cancellationToken) =>
        _db.Payments.FirstOrDefaultAsync(p => p.ProviderPrepareId == providerPrepareId, cancellationToken);

    public async Task SaveAsync(Payment payment, CancellationToken cancellationToken)
    {
        var exists = await _db.Payments.AnyAsync(p => p.Id == payment.Id, cancellationToken);
        if (exists)
            _db.Payments.Update(payment);
        else
            await _db.Payments.AddAsync(payment, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
