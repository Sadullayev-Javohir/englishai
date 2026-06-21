using Application.Identity.Ports;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

/// <summary>EF Core / PostgreSQL adapter for <see cref="IUserPreferencesStore"/>.</summary>
public sealed class EfUserPreferencesStore : IUserPreferencesStore
{
    private readonly EnglishAiDbContext _db;

    public EfUserPreferencesStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        var exists = await _db.UserPreferences
            .AsNoTracking()
            .AnyAsync(p => p.UserId == preferences.UserId, cancellationToken);

        if (exists)
            _db.UserPreferences.Update(preferences);
        else
            await _db.UserPreferences.AddAsync(preferences, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
