using Application.Notifications.Ports;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="INotificationArrivalStore"/>. Records the first-seen
/// time of each derived reminder occurrence so it shows a real, stable arrival time.
/// </summary>
public sealed class EfNotificationArrivalStore : INotificationArrivalStore
{
    private readonly EnglishAiDbContext _db;

    public EfNotificationArrivalStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, DateTimeOffset>> GetOrCreateAsync(
        IReadOnlyCollection<string> keys, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
            return new Dictionary<string, DateTimeOffset>();

        var distinct = keys.Distinct(StringComparer.Ordinal).ToArray();

        var existing = await _db.NotificationArrivals
            .Where(a => distinct.Contains(a.Key))
            .ToDictionaryAsync(a => a.Key, a => a.FirstSeenAt, cancellationToken);

        var missing = distinct.Where(k => !existing.ContainsKey(k)).ToArray();
        if (missing.Length > 0)
        {
            foreach (var key in missing)
            {
                await _db.NotificationArrivals.AddAsync(NotificationArrival.Create(key, now), cancellationToken);
                existing[key] = now;
            }

            // A concurrent request may insert the same key first; if so, keep the already-stored time
            // (the earliest arrival is the true one) rather than failing the read.
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var reloaded = await _db.NotificationArrivals
                    .AsNoTracking()
                    .Where(a => missing.Contains(a.Key))
                    .ToDictionaryAsync(a => a.Key, a => a.FirstSeenAt, cancellationToken);
                foreach (var (key, seen) in reloaded)
                    existing[key] = seen;
            }
        }

        return existing;
    }
}
