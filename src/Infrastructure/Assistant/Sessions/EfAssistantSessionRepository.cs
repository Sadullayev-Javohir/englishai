using Application.Assistant.Sessions;
using Domain.Assistant;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Assistant.Sessions;

public sealed class EfAssistantSessionRepository(EnglishAiDbContext db) : IAssistantSessionRepository
{
    public async Task<IReadOnlyList<AssistantSession>> ListActiveAsync(
        Guid learnerId, DateTimeOffset now, DateTimeOffset? beforeUpdatedAt, Guid? beforeId,
        int limit, CancellationToken cancellationToken)
    {
        var query = db.AssistantSessions.AsNoTracking()
            .Where(x => x.LearnerId == learnerId && x.ExpiresAt > now);
        if (beforeUpdatedAt.HasValue && beforeId.HasValue)
            query = query.Where(x => x.UpdatedAt < beforeUpdatedAt.Value
                || x.UpdatedAt == beforeUpdatedAt.Value && x.Id.CompareTo(beforeId.Value) < 0);

        return await query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id)
            .Take(limit).ToListAsync(cancellationToken);
    }

    public Task<AssistantSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken) =>
        db.AssistantSessions.Include(x => x.Messages).SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

    public Task AddAsync(AssistantSession session, CancellationToken cancellationToken) => db.AssistantSessions.AddAsync(session, cancellationToken).AsTask();
    public Task DeleteAsync(AssistantSession session, CancellationToken cancellationToken) { db.AssistantSessions.Remove(session); return Task.CompletedTask; }
    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken) => db.AssistantSessions.Where(x => x.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

public sealed class AssistantSessionCleanupJob(IAssistantSessionRepository sessions, TimeProvider timeProvider, ILogger<AssistantSessionCleanupJob> logger)
{
    public async Task RunAsync()
    {
        var deleted = await sessions.DeleteExpiredAsync(timeProvider.GetUtcNow(), CancellationToken.None);
        logger.LogInformation("Deleted {Count} expired assistant sessions.", deleted);
    }
}
