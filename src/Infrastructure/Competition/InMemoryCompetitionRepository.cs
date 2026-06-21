using System.Collections.Concurrent;
using Application.Competition.Ports;
using Domain.Competition;

namespace Infrastructure.Competition;

/// <summary>
/// In-memory stand-in for <see cref="ICompetitionRepository"/> so the Web host can boot and the
/// Level Map / other read endpoints remain available while the EF Core competition adapter is
/// completed. Competitions created here live only for the process lifetime and are NOT persisted.
/// This is intentionally minimal - it satisfies DI validation and the competition hubs; full
/// persistence is out of scope for the current task (frontend design work).
/// </summary>
public sealed class InMemoryCompetitionRepository : ICompetitionRepository
{
    private readonly ConcurrentDictionary<Guid, Domain.Competition.Competition> _store = new();

    public Task<Domain.Competition.Competition?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_store.TryGetValue(id, out var c) ? c : null);

    public Task AddAsync(Domain.Competition.Competition competition, CancellationToken cancellationToken)
    {
        _store[competition.Id] = competition;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Domain.Competition.Competition competition, CancellationToken cancellationToken)
    {
        _store[competition.Id] = competition;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Domain.Competition.Competition>> ListByHostAsync(Guid hostLearnerId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Domain.Competition.Competition>>(
            _store.Values.Where(c => c.HostLearnerId == hostLearnerId).ToList());

    public Task<IReadOnlyList<Domain.Competition.Competition>> ListActiveAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Domain.Competition.Competition>>(
            _store.Values.ToList());
}
