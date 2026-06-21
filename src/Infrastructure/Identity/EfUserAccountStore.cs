using Application.Identity.Ports;
using Domain.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IUserAccountStore"/>. Durable user
/// registration so a Google account always resumes the same learner progress.
/// </summary>
public sealed class EfUserAccountStore : IUserAccountStore
{
    private readonly EnglishAiDbContext _db;

    public EfUserAccountStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public Task<UserAccount?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken) =>
        _db.UserAccounts.FirstOrDefaultAsync(a => a.GoogleSubject == googleSubject, cancellationToken);

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.UserAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserAccount>> GetManyByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await _db.UserAccounts
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserAccount>> GetPageAsync(
        DateTimeOffset? beforeCreatedAt, Guid? beforeId, int limit, CancellationToken cancellationToken)
    {
        var query = _db.UserAccounts.AsNoTracking();
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
            query = query.Where(a => a.CreatedAt < beforeCreatedAt.Value
                || a.CreatedAt == beforeCreatedAt.Value && a.Id.CompareTo(beforeId.Value) < 0);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.UserAccounts.AsNoTracking().OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        _db.UserAccounts.CountAsync(cancellationToken);

    public Task<UserAccount?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        _db.UserAccounts.FirstOrDefaultAsync(a => a.Username == normalizedUsername, cancellationToken);

    public Task<UserAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        _db.UserAccounts.FirstOrDefaultAsync(a => a.Email.ToLower() == normalizedEmail, cancellationToken);

    public async Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        await _db.UserAccounts.AddAsync(account, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        // A loaded account is already tracked; guard the detached case defensively.
        if (_db.Entry(account).State == EntityState.Detached)
            _db.UserAccounts.Update(account);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserAccount account, CancellationToken cancellationToken)
    {
        _db.UserAccounts.Remove(account);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
