using Application.Analytics.Ports;
using Domain.Analytics;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Analytics;

/// <summary>PostgreSQL-backed product-event log.</summary>
public sealed class EfProductEventStore : IProductEventStore
{
    private readonly EnglishAiDbContext _db;

    public EfProductEventStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task AppendAsync(ProductEvent productEvent, CancellationToken cancellationToken)
    {
        _db.ProductEvents.Add(productEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendOnceAsync(
        Guid learnerId,
        ProductEventType type,
        DateTimeOffset occurredAt,
        string? source,
        CancellationToken cancellationToken)
    {
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        if (await _db.ProductEvents.AnyAsync(
                e => e.LearnerId == learnerId && e.Type == type && e.Source == normalizedSource,
                cancellationToken))
        {
            return;
        }

        _db.ProductEvents.Add(ProductEvent.Record(learnerId, type, occurredAt, normalizedSource));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductEvent>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.ProductEvents.AsNoTracking().ToListAsync(cancellationToken);
}
