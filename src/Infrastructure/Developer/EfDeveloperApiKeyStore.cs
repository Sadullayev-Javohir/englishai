using Application.Developer.Ports;
using Domain.Developer;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Developer;

public sealed class EfDeveloperApiKeyStore : IDeveloperApiKeyStore
{
    private readonly EnglishAiDbContext _db;

    public EfDeveloperApiKeyStore(EnglishAiDbContext db) => _db = db;

    public async Task AddAsync(DeveloperApiKey apiKey, CancellationToken cancellationToken)
    {
        await _db.DeveloperApiKeys.AddAsync(apiKey, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeveloperApiKey>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await _db.DeveloperApiKeys
            .AsNoTracking()
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<DeveloperApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.DeveloperApiKeys.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DeveloperApiKey>> GetActiveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken) =>
        await _db.DeveloperApiKeys
            .Where(k => k.Prefix == prefix && k.RevokedAt == null)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(DeveloperApiKey apiKey, CancellationToken cancellationToken)
    {
        if (_db.Entry(apiKey).State == EntityState.Detached)
            _db.DeveloperApiKeys.Update(apiKey);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.DeveloperApiKeys.Where(k => k.UserId == userId).ExecuteDeleteAsync(cancellationToken);
}
