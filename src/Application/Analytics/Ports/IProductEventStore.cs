using Domain.Analytics;

namespace Application.Analytics.Ports;

/// <summary>
/// Durable product-event log for activation analytics. Application handlers append milestone facts;
/// founder analytics reads them in bulk to build the activation funnel. Implementations provide an
/// idempotent append helper for session/page-view events that may be retried by the client.
/// </summary>
public interface IProductEventStore
{
    Task AppendAsync(ProductEvent productEvent, CancellationToken cancellationToken);

    Task AppendOnceAsync(
        Guid learnerId,
        ProductEventType type,
        DateTimeOffset occurredAt,
        string? source,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductEvent>> GetAllAsync(CancellationToken cancellationToken);
}
