using System.Collections.Concurrent;
using Application.Notifications.Ports;

namespace Infrastructure.Notifications;

/// <summary>
/// Dev/test <see cref="INotificationReadStateStore"/>: keeps each learner's read watermark in
/// memory. Process-local and resets on restart - acceptable without a database, since the EF
/// adapter provides durability when one is configured (docs/development-guide.md rule 10).
/// </summary>
public sealed class InMemoryNotificationReadStateStore : INotificationReadStateStore
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastReadAt = new();

    public Task<DateTimeOffset?> GetLastReadAtAsync(Guid learnerId, CancellationToken cancellationToken) =>
        Task.FromResult(_lastReadAt.TryGetValue(learnerId, out var at) ? at : (DateTimeOffset?)null);

    public Task SetLastReadAtAsync(Guid learnerId, DateTimeOffset lastReadAt, CancellationToken cancellationToken)
    {
        _lastReadAt.AddOrUpdate(learnerId, lastReadAt, (_, existing) => lastReadAt > existing ? lastReadAt : existing);
        return Task.CompletedTask;
    }
}
