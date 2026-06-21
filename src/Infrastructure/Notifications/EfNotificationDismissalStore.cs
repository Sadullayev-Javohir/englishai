using Application.Notifications.Ports;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="INotificationDismissalStore"/>. One durable row per
/// (learner, notification) so a tapped notification stays dismissed across restarts.
/// </summary>
public sealed class EfNotificationDismissalStore : INotificationDismissalStore
{
    private readonly EnglishAiDbContext _db;

    public EfNotificationDismissalStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<Guid>> GetDismissedAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        return await _db.NotificationDismissals
            .Where(d => d.LearnerId == learnerId)
            .Select(d => d.NotificationId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task DismissAsync(
        Guid learnerId, Guid notificationId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var exists = await _db.NotificationDismissals
            .AnyAsync(d => d.LearnerId == learnerId && d.NotificationId == notificationId, cancellationToken);
        if (exists)
            return;

        await _db.NotificationDismissals.AddAsync(
            NotificationDismissal.Create(learnerId, notificationId, now), cancellationToken);

        // A concurrent double-tap may insert the same pair first; the composite key makes that a
        // duplicate - swallow it since the dismissal is already recorded.
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
        }
    }
}
