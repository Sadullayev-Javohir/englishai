using System.Collections.Concurrent;
using Application.Developer.Ports;
using Domain.Developer;

namespace Infrastructure.Developer;

public sealed class InMemoryDeveloperApiKeyStore : IDeveloperApiKeyStore
{
    private readonly ConcurrentDictionary<Guid, DeveloperApiKey> _keys = new();

    public Task AddAsync(DeveloperApiKey apiKey, CancellationToken cancellationToken)
    {
        _keys[apiKey.Id] = apiKey;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeveloperApiKey>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DeveloperApiKey>>(
            _keys.Values.Where(k => k.UserId == userId).OrderByDescending(k => k.CreatedAt).ToList());

    public Task<DeveloperApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_keys.TryGetValue(id, out var key) ? key : null);

    public Task<IReadOnlyList<DeveloperApiKey>> GetActiveByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DeveloperApiKey>>(
            _keys.Values.Where(k => k.IsActive && k.Prefix == prefix).ToList());

    public Task UpdateAsync(DeveloperApiKey apiKey, CancellationToken cancellationToken)
    {
        _keys[apiKey.Id] = apiKey;
        return Task.CompletedTask;
    }

    public Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var key in _keys.Values.Where(k => k.UserId == userId))
            _keys.TryRemove(key.Id, out _);
        return Task.CompletedTask;
    }
}
