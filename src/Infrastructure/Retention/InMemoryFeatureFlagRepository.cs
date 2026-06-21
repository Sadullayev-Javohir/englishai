using System.Collections.Concurrent;
using Application.Retention.Ports;
using Domain.Retention;

namespace Infrastructure.Retention;

/// <summary>
/// In-memory <see cref="IFeatureFlagRepository"/> for dev/tests and for running the app
/// without a database. Seeded with the starter experiments (<see cref="FeatureFlagSeed"/>).
/// </summary>
public sealed class InMemoryFeatureFlagRepository : IFeatureFlagRepository
{
    private readonly ConcurrentDictionary<string, FeatureFlag> _flags = new();

    public InMemoryFeatureFlagRepository()
        : this(FeatureFlagSeed.Flags())
    {
    }

    // Parameter type is IReadOnlyList (not IEnumerable) so the DI container cannot satisfy
    // it and falls back to the seeded parameterless constructor.
    public InMemoryFeatureFlagRepository(IReadOnlyList<FeatureFlag> seed)
    {
        foreach (var flag in seed)
            _flags[flag.Key] = flag;
    }

    public Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_flags.TryGetValue(key, out var flag) ? flag : null);

    public Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FeatureFlag>>(_flags.Values.ToList());
}
