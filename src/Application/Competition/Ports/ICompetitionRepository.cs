namespace Application.Competition.Ports;

/// <summary>
/// Persistence port for the <see cref="Domain.Competition.Competition"/> aggregate. Implemented by an EF Core
/// adapter (PostgreSQL) that loads the full aggregate (slides, participants and their answers)
/// so the real-time game logic can run entirely in memory on the tracked entity.
/// </summary>
public interface ICompetitionRepository
{
    /// <summary>Returns the competition with its slides, participants and answers, or <c>null</c>.</summary>
    Task<Domain.Competition.Competition?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Inserts a new competition (with its built slides and host participant) and persists.</summary>
    Task AddAsync(Domain.Competition.Competition competition, CancellationToken cancellationToken);

    /// <summary>Persists mutations to a tracked competition (join, start, answer, advance, finish).</summary>
    Task UpdateAsync(Domain.Competition.Competition competition, CancellationToken cancellationToken);

    /// <summary>All competitions hosted by the given learner.</summary>
    Task<IReadOnlyList<Domain.Competition.Competition>> ListByHostAsync(Guid hostLearnerId, CancellationToken cancellationToken);

    /// <summary>All currently active (in-progress) competitions.</summary>
    Task<IReadOnlyList<Domain.Competition.Competition>> ListActiveAsync(CancellationToken cancellationToken);
}
