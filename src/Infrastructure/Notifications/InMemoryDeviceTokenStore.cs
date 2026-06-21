using System.Collections.Concurrent;
using Application.Notifications.Ports;
using Domain.Notifications;

namespace Infrastructure.Notifications;

/// <summary>
/// Process-local <see cref="IDeviceTokenStore"/> for dev/test hosts without a database. Keyed by the
/// token so re-registration upserts rather than duplicates. State resets on restart - acceptable
/// when there is no database (the same context that leaves other stores in-memory).
/// </summary>
public sealed class InMemoryDeviceTokenStore : IDeviceTokenStore
{
    private readonly ConcurrentDictionary<string, DeviceToken> _byToken = new(StringComparer.Ordinal);

    public Task UpsertAsync(
        Guid userAccountId, string token, string platform, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var trimmed = token.Trim();
        _byToken.AddOrUpdate(
            trimmed,
            _ => DeviceToken.Create(userAccountId, trimmed, platform, now),
            (_, existing) =>
            {
                existing.Refresh(userAccountId, platform, now);
                return existing;
            });
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeviceToken>> GetForUsersAsync(
        IReadOnlyCollection<Guid> userAccountIds, CancellationToken cancellationToken)
    {
        var ids = userAccountIds.ToHashSet();
        IReadOnlyList<DeviceToken> result = _byToken.Values.Where(d => ids.Contains(d.UserAccountId)).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DeviceToken>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DeviceToken> result = _byToken.Values.ToList();
        return Task.FromResult(result);
    }

    public Task RemoveAsync(IReadOnlyCollection<string> tokens, CancellationToken cancellationToken)
    {
        foreach (var token in tokens)
            _byToken.TryRemove(token, out _);
        return Task.CompletedTask;
    }
}
