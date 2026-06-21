using Application.Notifications.Ports;
using Domain.Notifications;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications;

/// <summary>EF Core / PostgreSQL adapter for <see cref="IDeviceTokenStore"/>.</summary>
public sealed class EfDeviceTokenStore : IDeviceTokenStore
{
    private readonly EnglishAiDbContext _db;

    public EfDeviceTokenStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task UpsertAsync(
        Guid userAccountId, string token, string platform, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var trimmed = token.Trim();
        var existing = await _db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == trimmed, cancellationToken);
        if (existing is null)
        {
            await _db.DeviceTokens.AddAsync(DeviceToken.Create(userAccountId, trimmed, platform, now), cancellationToken);
        }
        else
        {
            existing.Refresh(userAccountId, platform, now);
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent registration of the same token won the unique index; the device is already
            // recorded, so treat the race as success rather than surfacing an error to the client.
            _db.ChangeTracker.Clear();
        }
    }

    public async Task<IReadOnlyList<DeviceToken>> GetForUsersAsync(
        IReadOnlyCollection<Guid> userAccountIds, CancellationToken cancellationToken)
    {
        if (userAccountIds.Count == 0)
            return Array.Empty<DeviceToken>();

        var ids = userAccountIds.Distinct().ToArray();
        return await _db.DeviceTokens
            .AsNoTracking()
            .Where(d => ids.Contains(d.UserAccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceToken>> GetAllAsync(CancellationToken cancellationToken)
        => await _db.DeviceTokens.AsNoTracking().ToListAsync(cancellationToken);

    public async Task RemoveAsync(IReadOnlyCollection<string> tokens, CancellationToken cancellationToken)
    {
        if (tokens.Count == 0)
            return;

        var set = tokens.ToArray();
        await _db.DeviceTokens
            .Where(d => set.Contains(d.Token))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
