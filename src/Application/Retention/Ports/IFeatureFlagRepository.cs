using Domain.Retention;

namespace Application.Retention.Ports;

/// <summary>
/// Read access to the feature-flag experiments (PROJECT-SPEC I.3). Backed by a seeded
/// in-memory adapter for dev/tests and an EF Core adapter when a database is configured.
/// Variant assignment is computed deterministically by the <see cref="FeatureFlag"/>
/// aggregate, so the repository only needs to load flag definitions.
/// </summary>
public interface IFeatureFlagRepository
{
    /// <summary>Returns the flag with the given key, or <c>null</c> if it is not defined.</summary>
    Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>All defined flags.</summary>
    Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken);
}
