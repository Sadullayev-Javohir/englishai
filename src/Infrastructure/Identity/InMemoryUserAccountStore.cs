using System.Collections.Concurrent;
using Application.Identity.Ports;
using Domain.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// In-memory <see cref="IUserAccountStore"/> for dev/tests and for running the app without
/// a database configured. Keyed by account id, with a secondary lookup by Google subject.
/// Registered as a singleton so accounts persist across requests within a process run.
/// </summary>
public sealed class InMemoryUserAccountStore : IUserAccountStore
{
    private readonly ConcurrentDictionary<Guid, UserAccount> _byId = new();

    public Task<UserAccount?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.FirstOrDefault(a => a.GoogleSubject == googleSubject));

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.TryGetValue(id, out var account) ? account : null);

    public Task<IReadOnlyList<UserAccount>> GetManyByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(
            _byId.Values.Where(a => ids.Contains(a.Id)).ToList());

    public Task<IReadOnlyList<UserAccount>> GetPageAsync(
        DateTimeOffset? beforeCreatedAt, Guid? beforeId, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(_byId.Values
            .Where(a => !beforeCreatedAt.HasValue || !beforeId.HasValue
                || a.CreatedAt < beforeCreatedAt.Value
                || a.CreatedAt == beforeCreatedAt.Value && a.Id.CompareTo(beforeId.Value) < 0)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(limit)
            .ToList());

    public Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(
            _byId.Values.OrderByDescending(a => a.CreatedAt).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Count);

    public Task<UserAccount?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.FirstOrDefault(a => a.Username == normalizedUsername));

    public Task<UserAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.Values.FirstOrDefault(
            a => string.Equals(a.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        _byId[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        _byId[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserAccount account, CancellationToken cancellationToken)
    {
        _byId.TryRemove(account.Id, out _);
        return Task.CompletedTask;
    }
}
