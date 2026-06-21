using System.Collections.Concurrent;
using Application.Analytics.Ports;
using Domain.Analytics;

namespace Infrastructure.Analytics;

/// <summary>Process-local product-event log for dev/test runs without PostgreSQL.</summary>
public sealed class InMemoryProductEventStore : IProductEventStore
{
    private readonly ConcurrentDictionary<Guid, ProductEvent> _events = new();
    private readonly object _gate = new();

    public Task AppendAsync(ProductEvent productEvent, CancellationToken cancellationToken)
    {
        _events[productEvent.Id] = productEvent;
        return Task.CompletedTask;
    }

    public Task AppendOnceAsync(
        Guid learnerId,
        ProductEventType type,
        DateTimeOffset occurredAt,
        string? source,
        CancellationToken cancellationToken)
    {
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        lock (_gate)
        {
            if (_events.Values.Any(e => e.LearnerId == learnerId && e.Type == type && e.Source == normalizedSource))
                return Task.CompletedTask;

            var productEvent = ProductEvent.Record(learnerId, type, occurredAt, normalizedSource);
            _events[productEvent.Id] = productEvent;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProductEvent>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductEvent> snapshot = _events.Values
            .OrderBy(e => e.OccurredAt)
            .ToList();
        return Task.FromResult(snapshot);
    }
}
