using System.Collections.Concurrent;
using Application.Subscription.Ports;
using Domain.Subscription;

namespace Infrastructure.Subscription;

/// <summary>Dev/test stand-in for <see cref="IPremiumWaitlistRepository"/>, keyed like the unique index.</summary>
public sealed class InMemoryPremiumWaitlistRepository : IPremiumWaitlistRepository
{
    private readonly ConcurrentDictionary<string, PremiumWaitlistEntry> _entries =
        new(StringComparer.Ordinal);

    public Task<PremiumWaitlistEntry?> GetByContactAsync(string contact, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.TryGetValue(contact, out var entry) ? entry : null);

    public Task AddAsync(PremiumWaitlistEntry entry, CancellationToken cancellationToken)
    {
        _entries.TryAdd(entry.Contact, entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PremiumWaitlistEntry>> GetRecentAsync(
        int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PremiumWaitlistEntry>>(_entries.Values
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken) => Task.FromResult(_entries.Count);
}
