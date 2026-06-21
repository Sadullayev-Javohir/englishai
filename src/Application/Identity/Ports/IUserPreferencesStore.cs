using Domain.Identity;

namespace Application.Identity.Ports;

/// <summary>
/// Persistence port for <see cref="UserPreferences"/>. Implemented by a durable EF Core adapter
/// when a database is configured, and an in-memory adapter otherwise.
/// </summary>
public interface IUserPreferencesStore
{
    /// <summary>The learner's saved preferences, or null if they have never changed a setting.</summary>
    Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Inserts or updates the learner's preferences.</summary>
    Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken);
}
