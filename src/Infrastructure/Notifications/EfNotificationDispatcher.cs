using Application.Notifications.Ports;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

public sealed class EfNotificationDispatcher(EnglishAiDbContext db) : INotificationDispatcher
{
    public async Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        var duplicate = await db.Set<Notification>().AnyAsync(existing =>
            existing.LearnerId == notification.LearnerId &&
            existing.Code == notification.Code &&
            existing.CreatedAt == notification.CreatedAt,
            cancellationToken);
        if (duplicate)
            return;

        db.Set<Notification>().Add(notification);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(notification).State = EntityState.Detached;
            var persisted = await db.Set<Notification>().AsNoTracking().AnyAsync(existing =>
                existing.LearnerId == notification.LearnerId &&
                existing.Code == notification.Code &&
                existing.CreatedAt == notification.CreatedAt,
                cancellationToken);
            if (!persisted)
                throw;
        }
    }

    public async Task<IReadOnlyList<Notification>> GetForLearnerAsync(Guid learnerId, CancellationToken cancellationToken) =>
        await db.Set<Notification>()
            .Where(notification => notification.LearnerId == learnerId)
            .OrderByDescending(notification => notification.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task MarkAllReadAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        await db.Set<Notification>()
            .Where(notification => notification.LearnerId == learnerId && !notification.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.IsRead, true), cancellationToken);
    }
}
