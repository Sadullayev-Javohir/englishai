using Application.Notifications.Ports;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="INotificationReadStateStore"/>. One durable row per
/// learner holding the "mark all read" watermark, so dismissing notifications survives restarts.
/// </summary>
public sealed class EfNotificationReadStateStore : INotificationReadStateStore
{
    private readonly EnglishAiDbContext _db;

    public EfNotificationReadStateStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<DateTimeOffset?> GetLastReadAtAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        var state = await _db.NotificationReadStates
            .FirstOrDefaultAsync(s => s.LearnerId == learnerId, cancellationToken);
        return state?.LastReadAt;
    }

    public async Task SetLastReadAtAsync(
        Guid learnerId, DateTimeOffset lastReadAt, CancellationToken cancellationToken)
    {
        var state = await _db.NotificationReadStates
            .FirstOrDefaultAsync(s => s.LearnerId == learnerId, cancellationToken);

        if (state is null)
        {
            state = NotificationReadState.Create(learnerId, lastReadAt);
            await _db.NotificationReadStates.AddAsync(state, cancellationToken);
        }
        else
        {
            state.MarkReadAt(lastReadAt);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
