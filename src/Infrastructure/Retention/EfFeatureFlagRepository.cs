using Application.Retention.Ports;
using Domain.Retention;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Retention;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IFeatureFlagRepository"/>. The owned variant
/// collection loads with each flag. Like the other curated catalogs, the flag definitions
/// are seeded out of band (admin tooling / migration) rather than auto-populated here.
/// </summary>
public sealed class EfFeatureFlagRepository : IFeatureFlagRepository
{
    private readonly EnglishAiDbContext _db;

    public EfFeatureFlagRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
        await _db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key, cancellationToken);

    public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.FeatureFlags.ToListAsync(cancellationToken);
}
