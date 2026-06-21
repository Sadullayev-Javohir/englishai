using Domain.Learning;

namespace Application.Learning.Ports;

/// <summary>
/// Persistence port for the <see cref="LearnerProfile"/> aggregate. Implemented by an
/// EF Core adapter (PostgreSQL) in production and an in-memory adapter for dev/tests.
/// </summary>
public interface ILearnerProfileRepository
{
    /// <summary>Returns the profile for a learner, or <c>null</c> if none exists yet.</summary>
    Task<LearnerProfile?> GetByLearnerIdAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>
    /// All learner profiles, for the daily retention sweep and cohort-metrics queries
    /// (PROJECT-SPEC I.2/I.4). Reads only; callers persist via <see cref="SaveAsync"/>.
    /// </summary>
    Task<IReadOnlyList<LearnerProfile>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Inserts a new profile or updates the existing one for its learner.</summary>
    Task SaveAsync(LearnerProfile profile, CancellationToken cancellationToken);

    /// <summary>
    /// Tracks a profile mutation without committing it immediately. This is used by workflows
    /// that must persist the profile together with another aggregate in one DbContext commit.
    /// Implementations without deferred persistence may fall back to <see cref="SaveAsync"/>.
    /// </summary>
    Task TrackAsync(LearnerProfile profile, CancellationToken cancellationToken) =>
        SaveAsync(profile, cancellationToken);
}
