using System.Collections.Concurrent;
using Application.Learning.Ports;
using Domain.Learning;

namespace Infrastructure.Learning;

/// <summary>
/// In-memory <see cref="ILearnerProfileRepository"/> for dev/tests and for running
/// the app without a database configured. Stores the live aggregate instance keyed by
/// learner id, so in-place mutations are reflected on the next read.
/// </summary>
public sealed class InMemoryLearnerProfileRepository : ILearnerProfileRepository
{
    private readonly ConcurrentDictionary<Guid, LearnerProfile> _profiles = new();

    public Task<LearnerProfile?> GetByLearnerIdAsync(Guid learnerId, CancellationToken cancellationToken) =>
        Task.FromResult(_profiles.TryGetValue(learnerId, out var profile) ? profile : null);

    public Task<IReadOnlyList<LearnerProfile>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LearnerProfile>>(_profiles.Values.ToList());

    public Task SaveAsync(LearnerProfile profile, CancellationToken cancellationToken)
    {
        _profiles[profile.LearnerId] = profile;
        return Task.CompletedTask;
    }
}
