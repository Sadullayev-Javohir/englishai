using Domain.Identity;

namespace Application.Identity.Ports;

/// <summary>
/// Persistence port for <see cref="UserAccount"/> aggregates. Implemented by a durable
/// EF Core adapter when a database is configured, and an in-memory adapter otherwise.
/// </summary>
public interface IUserAccountStore
{
    /// <summary>Finds the account linked to a Google subject, or null if none exists yet.</summary>
    Task<UserAccount?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken);

    /// <summary>Finds an account by its id (which doubles as the learner id), or null.</summary>
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Finds every account whose id is in <paramref name="ids"/>, in no particular order.
    /// Used to bulk-resolve display name/avatar for a page of results (e.g. the leaderboard)
    /// without one round-trip per row.
    /// </summary>
    Task<IReadOnlyList<UserAccount>> GetManyByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// A bounded page of registered accounts, newest registration first with id as a stable tie-breaker.
    /// </summary>
    Task<IReadOnlyList<UserAccount>> GetPageAsync(
        DateTimeOffset? beforeCreatedAt, Guid? beforeId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Counts all registered accounts without loading them into memory.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Finds the account that owns a (normalized, lowercase) username, or null if it is free.
    /// Used to enforce case-insensitive uniqueness when a username is claimed or changed.
    /// </summary>
    Task<UserAccount?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the account that owns a (normalized, lowercase) email, or null if none does. Used
    /// to stop a Google login from registering a second account under an email another account
    /// already owns (a different Google subject reporting the same address).
    /// </summary>
    Task<UserAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Persists a newly registered account.</summary>
    Task AddAsync(UserAccount account, CancellationToken cancellationToken);

    /// <summary>Persists profile/login changes to an existing account.</summary>
    Task UpdateAsync(UserAccount account, CancellationToken cancellationToken);

    /// <summary>Permanently removes an account (used when the user deletes their account).</summary>
    Task DeleteAsync(UserAccount account, CancellationToken cancellationToken);
}
