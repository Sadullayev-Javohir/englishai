using Domain.Assistant;

namespace Application.Assistant.Sessions;

public sealed record AssistantSessionSummaryDto(Guid Id, string Skill, string ResourceType, string? ResourceId, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset ExpiresAt);
public sealed record AssistantMessageSourceDto(string Area, string ResourceType, string ResourceId, string Title, string Route);
public sealed record AssistantMessageDto(Guid Id, string Role, string Text, string Status, string Source, Guid? ClientRequestId,
    long? LatencyMs, DateTimeOffset CreatedAt, IReadOnlyList<AssistantMessageSource> Sources);
public sealed record AssistantSessionDto(Guid Id, string Skill, string ResourceType, string? ResourceId, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset ExpiresAt, IReadOnlyList<AssistantMessageDto> Messages);
public sealed record AssistantSessionPageDto(IReadOnlyList<AssistantSessionSummaryDto> Items, string? NextCursor);

public interface IAssistantSessionRepository
{
    Task<IReadOnlyList<AssistantSession>> ListActiveAsync(
        Guid learnerId, DateTimeOffset now, DateTimeOffset? beforeUpdatedAt, Guid? beforeId,
        int limit, CancellationToken cancellationToken);
    Task<AssistantSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken);
    Task AddAsync(AssistantSession session, CancellationToken cancellationToken);
    Task DeleteAsync(AssistantSession session, CancellationToken cancellationToken);
    Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public static class AssistantSessionMappings
{
    public static AssistantSessionSummaryDto ToSummary(this AssistantSession session) => new(session.Id, session.Skill, session.ResourceType, session.ResourceId, session.Title, session.CreatedAt, session.UpdatedAt, session.ExpiresAt);
    public static AssistantSessionDto ToDto(this AssistantSession session) => new(session.Id, session.Skill, session.ResourceType, session.ResourceId, session.Title, session.CreatedAt, session.UpdatedAt, session.ExpiresAt,
        session.Messages.OrderBy(x => x.CreatedAt).Select(x => new AssistantMessageDto(x.Id, x.Role, x.Text, x.Status,
            x.Source, x.ClientRequestId, x.LatencyMs, x.CreatedAt, AssistantSourceJson.Deserialize(x.SourceMetadataJson))).ToArray());
}
