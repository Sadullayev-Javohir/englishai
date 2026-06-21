using Application.Assistant.Ports;
using Application.Common;
using Domain.Assistant;
using Domain.Common;
using System.Diagnostics;

namespace Application.Assistant.Sessions;

public sealed record CreateAssistantSessionRequest(string Skill, string ResourceType, string? ResourceId, string Title);
public sealed record SendAssistantMessageRequest(
    string Question,
    string? Context,
    string? FocusText,
    Guid ClientRequestId,
    string? Route = null,
    string? Stage = null);
public sealed record AssistantMessageResultDto(AssistantMessageDto UserMessage, AssistantMessageDto AssistantMessage);

public interface IAssistantSessionService
{
    Task<AssistantSessionPageDto> ListAsync(string? cursor, int pageSize, CancellationToken cancellationToken);
    Task<AssistantSessionDto> CreateAsync(CreateAssistantSessionRequest request, CancellationToken cancellationToken);
    Task<AssistantSessionDto> GetAsync(Guid sessionId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<AssistantSessionDto> RenameAsync(Guid sessionId, string title, CancellationToken cancellationToken);
    Task<AssistantMessageResultDto> SendAsync(Guid sessionId, SendAssistantMessageRequest request, CancellationToken cancellationToken);
}

public sealed class AssistantSessionService(
    IAssistantSessionRepository sessions,
    IContextualAssistantCoordinator coordinator,
    IAssistantKnowledgeRetriever knowledge,
    ICurrentUserAccessor currentUser,
    TimeProvider timeProvider) : IAssistantSessionService
{
    private static readonly HashSet<string> Skills = new(StringComparer.OrdinalIgnoreCase)
    { "general", "vocabulary", "grammar", "reading", "writing", "speaking", "listening", "video", "books", "progress" };
    private static readonly HashSet<string> ResourceTypes = new(StringComparer.OrdinalIgnoreCase)
    { "page", "topic", "lesson", "video", "book", "section" };

    public async Task<AssistantSessionPageDto> ListAsync(string? cursorValue, int pageSize, CancellationToken cancellationToken)
    {
        var learnerId = RequireLearner();
        pageSize = Math.Clamp(pageSize, 1, 50);
        var cursor = StableCursor.Decode(cursorValue);
        var page = await sessions.ListActiveAsync(
            learnerId, timeProvider.GetUtcNow(), cursor?.Timestamp, cursor?.Id, pageSize + 1, cancellationToken);
        var items = page.Take(pageSize).Select(x => x.ToSummary()).ToArray();
        return new AssistantSessionPageDto(items, page.Count > pageSize
            ? new StableCursor(page[pageSize - 1].UpdatedAt, page[pageSize - 1].Id).Encode()
            : null);
    }

    public async Task<AssistantSessionDto> CreateAsync(CreateAssistantSessionRequest request, CancellationToken cancellationToken)
    {
        if (!Skills.Contains(request.Skill)) throw new ArgumentException("Unsupported assistant skill.", nameof(request.Skill));
        if (!ResourceTypes.Contains(request.ResourceType)) throw new ArgumentException("Unsupported assistant resource type.", nameof(request.ResourceType));
        var session = AssistantSession.Create(RequireLearner(), request.Skill, request.ResourceType, request.ResourceId, request.Title, timeProvider.GetUtcNow());
        await sessions.AddAsync(session, cancellationToken);
        await sessions.SaveChangesAsync(cancellationToken);
        return session.ToDto();
    }

    public async Task<AssistantSessionDto> GetAsync(Guid sessionId, CancellationToken cancellationToken) =>
        (await GetOwnedActiveAsync(sessionId, cancellationToken)).ToDto();

    public async Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await GetOwnedAsync(sessionId, cancellationToken);
        await sessions.DeleteAsync(session, cancellationToken);
        await sessions.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssistantSessionDto> RenameAsync(Guid sessionId, string title, CancellationToken cancellationToken)
    {
        var session = await GetOwnedActiveAsync(sessionId, cancellationToken);
        session.Rename(title, timeProvider.GetUtcNow());
        await sessions.SaveChangesAsync(cancellationToken);
        return session.ToDto();
    }

    public async Task<AssistantMessageResultDto> SendAsync(Guid sessionId, SendAssistantMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 1000)
            throw new ArgumentException("Question must contain 1-1000 characters.", nameof(request.Question));
        if (request.ClientRequestId == Guid.Empty)
            throw new ArgumentException("clientRequestId is required.", nameof(request.ClientRequestId));

        var session = await GetOwnedActiveAsync(sessionId, cancellationToken);
        var existingUser = session.Messages.FirstOrDefault(x => x.ClientRequestId == request.ClientRequestId && x.Role == "user");
        if (existingUser is not null)
        {
            var existingAssistant = session.Messages.FirstOrDefault(x => x.ClientRequestId == request.ClientRequestId && x.Role == "assistant");
            if (existingAssistant is not null) return new(ToDto(existingUser), ToDto(existingAssistant));
        }

        var now = timeProvider.GetUtcNow();
        var userMessage = existingUser ?? session.AddMessage("user", request.Question, "completed", "local", request.ClientRequestId, null, now);
        if (existingUser is null) await sessions.SaveChangesAsync(cancellationToken);

        var history = session.Messages.Where(x => x.Id != userMessage.Id).OrderBy(x => x.CreatedAt).TakeLast(6)
            .Select(x => new ContextualAssistantTurn(x.Role, x.Text)).ToArray();
        var normalizedQuestion = string.Join(' ', request.Question.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var pageContext = Limit(request.Context, 4000);
        var boundedFocus = Limit(request.FocusText, 2000);
        var retrieved = await knowledge.RetrieveAsync(new AssistantKnowledgeRequest(
            session.LearnerId, session.Skill, session.ResourceType, session.ResourceId, session.Title,
            request.Question.Trim(), pageContext, boundedFocus), cancellationToken);
        var boundedContext = string.Join("\n\n", new[]
        {
            string.IsNullOrWhiteSpace(request.Route) ? string.Empty : $"CURRENT ROUTE: {Limit(request.Route, 300)}",
            string.IsNullOrWhiteSpace(request.Stage) ? string.Empty : $"CURRENT STAGE: {Limit(request.Stage, 100)}",
            pageContext,
            retrieved.Context,
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        boundedContext = Limit(boundedContext, 16000);
        var cacheKey = AssistantCacheKey.Create(session.Skill, session.ResourceId, boundedContext, boundedFocus, normalizedQuestion, history);
        var stopwatch = Stopwatch.StartNew();
        var reply = await coordinator.ExecuteAsync(new ContextualAssistantWork(cacheKey, session.Skill, session.Title, boundedContext,
            boundedFocus, request.Question.Trim(), history), session.LearnerId.ToString(), cancellationToken) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reply)) throw new AssistantUnavailableException();
        stopwatch.Stop();
        var assistantMessage = session.AddMessage("assistant", reply, "completed", "ai", request.ClientRequestId,
            stopwatch.ElapsedMilliseconds, timeProvider.GetUtcNow(), retrieved.Sources.Select(x =>
                new AssistantMessageSource(x.Area, x.ResourceType, x.ResourceId, x.Title, x.Route)).ToArray());
        await sessions.SaveChangesAsync(cancellationToken);
        return new(ToDto(userMessage), ToDto(assistantMessage));
    }

    private async Task<AssistantSession> GetOwnedActiveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await GetOwnedAsync(sessionId, cancellationToken);
        if (session.IsExpired(timeProvider.GetUtcNow())) throw new NotFoundException(nameof(AssistantSession), sessionId);
        return session;
    }

    private async Task<AssistantSession> GetOwnedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await sessions.GetAsync(sessionId, cancellationToken) ?? throw new NotFoundException(nameof(AssistantSession), sessionId);
        if (session.LearnerId != RequireLearner()) throw new ForbiddenException("Assistant session belongs to another learner.");
        return session;
    }

    private Guid RequireLearner() => currentUser.LearnerId ?? throw new ForbiddenException("Authentication is required.");
    private static string Limit(string? value, int max) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static AssistantMessageDto ToDto(AssistantMessage x) => new(x.Id, x.Role, x.Text, x.Status, x.Source,
        x.ClientRequestId, x.LatencyMs, x.CreatedAt, AssistantSourceJson.Deserialize(x.SourceMetadataJson));
}

internal static class AssistantCacheKey
{
    public static string Create(string skill, string? resourceId, string? context, string? focus, string question, IReadOnlyList<ContextualAssistantTurn> history)
    {
        var followUp = question is "again" or "more" || question.Contains("yana", StringComparison.OrdinalIgnoreCase) || question.Contains("buni", StringComparison.OrdinalIgnoreCase);
        var historyKey = followUp ? string.Join('|', history.TakeLast(4).Select(x => $"{x.Role}:{x.Text[..Math.Min(x.Text.Length, 500)]}")) : string.Empty;
        var contextHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Normalize(context))));
        var raw = $"v4|{skill}|{resourceId}|{contextHash}|{Normalize(focus)}|{question}|{historyKey}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
    }
    private static string Normalize(string? value) => string.Join(' ', (value ?? string.Empty).ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

public sealed class AssistantUnavailableException() : Exception("AI assistant is temporarily unavailable.")
{
    public string Code { get; } = "assistant_unavailable";
    public int RetryAfterSeconds { get; } = 5;
}
