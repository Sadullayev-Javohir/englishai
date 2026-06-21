using System.Collections.Concurrent;
using Application.Assistant.Sessions;
using Domain.Assistant;

namespace Infrastructure.Assistant.Sessions;

public sealed class InMemoryAssistantSessionRepository : IAssistantSessionRepository
{
    private readonly ConcurrentDictionary<Guid, AssistantSession> _sessions = new();
    public Task<IReadOnlyList<AssistantSession>> ListActiveAsync(
        Guid learnerId, DateTimeOffset now, DateTimeOffset? beforeUpdatedAt, Guid? beforeId,
        int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AssistantSession>>(_sessions.Values
            .Where(x => x.LearnerId == learnerId && !x.IsExpired(now))
            .Where(x => !beforeUpdatedAt.HasValue || !beforeId.HasValue
                || x.UpdatedAt < beforeUpdatedAt.Value
                || x.UpdatedAt == beforeUpdatedAt.Value && x.Id.CompareTo(beforeId.Value) < 0)
            .OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id)
            .Take(limit).ToArray());
    public Task<AssistantSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken) => Task.FromResult(_sessions.GetValueOrDefault(sessionId));
    public Task AddAsync(AssistantSession session, CancellationToken cancellationToken) { _sessions[session.Id] = session; return Task.CompletedTask; }
    public Task DeleteAsync(AssistantSession session, CancellationToken cancellationToken) { _sessions.TryRemove(session.Id, out _); return Task.CompletedTask; }
    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var deleted = 0;
        foreach (var item in _sessions.Where(x => x.Value.IsExpired(now)).ToArray()) if (_sessions.TryRemove(item.Key, out _)) deleted++;
        return Task.FromResult(deleted);
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
