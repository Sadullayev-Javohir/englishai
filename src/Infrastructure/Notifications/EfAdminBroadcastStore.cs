using Application.Notifications.Ports;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IAdminBroadcastStore"/>. One durable row per broadcast,
/// so a super-admin's notification survives restarts and reaches every learner's feed.
/// </summary>
public sealed class EfAdminBroadcastStore : IAdminBroadcastStore
{
    private readonly EnglishAiDbContext _db;

    public EfAdminBroadcastStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AdminBroadcast broadcast, CancellationToken cancellationToken)
    {
        await _db.AdminBroadcasts.AddAsync(broadcast, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminBroadcast>> GetSinceAsync(
        DateTimeOffset since, CancellationToken cancellationToken) =>
        await _db.AdminBroadcasts
            .AsNoTracking()
            .Where(b => b.CreatedAt >= since)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AdminBroadcast>> GetRecentAsync(
        DateTimeOffset? beforeCreatedAt, Guid? beforeId, int limit, CancellationToken cancellationToken)
    {
        var query = _db.AdminBroadcasts.AsNoTracking();
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
            query = query.Where(b => b.CreatedAt < beforeCreatedAt.Value
                || b.CreatedAt == beforeCreatedAt.Value && b.Id.CompareTo(beforeId.Value) < 0);

        return await query.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
            .Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var broadcast = await _db.AdminBroadcasts.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (broadcast is null)
            return false;

        _db.AdminBroadcasts.Remove(broadcast);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
