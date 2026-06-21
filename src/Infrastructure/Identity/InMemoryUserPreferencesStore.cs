using System.Collections.Concurrent;
using Application.Identity.Ports;
using Domain.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// In-memory <see cref="IUserPreferencesStore"/> for dev/tests and for running without a
/// database. Registered as a singleton so preferences persist across requests in a process run.
/// </summary>
public sealed class InMemoryUserPreferencesStore : IUserPreferencesStore
{
    private readonly ConcurrentDictionary<Guid, UserPreferences> _byUserId = new();

    public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_byUserId.TryGetValue(userId, out var preferences) ? preferences : null);

    public Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        _byUserId[preferences.UserId] = preferences;
        return Task.CompletedTask;
    }
}
