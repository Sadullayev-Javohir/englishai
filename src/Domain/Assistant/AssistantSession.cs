using Domain.Common;

namespace Domain.Assistant;

public sealed class AssistantSession
{
    private readonly List<AssistantMessage> _messages = new();

    private AssistantSession() { }

    private AssistantSession(Guid learnerId, string skill, string resourceType, string? resourceId, string title, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Skill = Normalize(skill, 32);
        ResourceType = Normalize(resourceType, 32);
        ResourceId = NormalizeOptional(resourceId, 200);
        Title = Normalize(title, 200);
        CreatedAt = now;
        UpdatedAt = now;
        ExpiresAt = now.AddHours(24);
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public string Skill { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string? ResourceId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public IReadOnlyList<AssistantMessage> Messages => _messages;

    public static AssistantSession Create(Guid learnerId, string skill, string resourceType, string? resourceId, string title, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty) throw new DomainException("Assistant session needs a learner id.");
        return new AssistantSession(learnerId, skill, resourceType, resourceId, title, now);
    }

    public void Rename(string title, DateTimeOffset now)
    {
        Title = Normalize(title, 200);
        Touch(now);
    }

    public AssistantMessage AddMessage(string role, string text, string status, string source, Guid? clientRequestId,
        long? latencyMs, DateTimeOffset now, IReadOnlyList<AssistantMessageSource>? sources = null)
    {
        if (clientRequestId.HasValue)
        {
            var existing = _messages.FirstOrDefault(x => x.ClientRequestId == clientRequestId && x.Role == role.Trim().ToLowerInvariant());
            if (existing is not null) return existing;
        }

        var message = AssistantMessage.Create(Id, role, text, status, source, clientRequestId, latencyMs, now, sources);
        _messages.Add(message);
        Touch(now);
        return message;
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        ExpiresAt = now.AddHours(24);
    }

    private static string Normalize(string value, int maxLength)
    {
        var normalized = string.Join(' ', (value ?? string.Empty).Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0) throw new DomainException("Assistant session value is required.");
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }
}

public sealed class AssistantMessage
{
    private AssistantMessage() { }

    private AssistantMessage(Guid sessionId, string role, string text, string status, string source, Guid? clientRequestId,
        long? latencyMs, DateTimeOffset now, IReadOnlyList<AssistantMessageSource>? sources)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        Role = role;
        Text = text;
        Status = status;
        Source = source;
        ClientRequestId = clientRequestId;
        LatencyMs = latencyMs;
        SourceMetadataJson = AssistantSourceJson.Serialize(sources);
        CreatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public Guid? ClientRequestId { get; private set; }
    public long? LatencyMs { get; private set; }
    public string SourceMetadataJson { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; private set; }

    internal static AssistantMessage Create(Guid sessionId, string role, string text, string status, string source,
        Guid? clientRequestId, long? latencyMs, DateTimeOffset now, IReadOnlyList<AssistantMessageSource>? sources)
    {
        role = role.Trim().ToLowerInvariant();
        status = status.Trim().ToLowerInvariant();
        source = source.Trim().ToLowerInvariant();
        if (role is not ("user" or "assistant")) throw new DomainException("Invalid assistant message role.");
        if (status is not ("completed" or "failed" or "fallback")) throw new DomainException("Invalid assistant message status.");
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Assistant message text is required.");
        return new AssistantMessage(sessionId, role, text.Trim(), status, source, clientRequestId, latencyMs, now, sources);
    }
}

public sealed record AssistantMessageSource(string Area, string ResourceType, string ResourceId, string Title, string Route);

public static class AssistantSourceJson
{
    public static string Serialize(IReadOnlyList<AssistantMessageSource>? sources) =>
        System.Text.Json.JsonSerializer.Serialize(sources ?? Array.Empty<AssistantMessageSource>());

    public static IReadOnlyList<AssistantMessageSource> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<AssistantMessageSource>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<AssistantMessageSource[]>(json)
                   ?? Array.Empty<AssistantMessageSource>();
        }
        catch (System.Text.Json.JsonException)
        {
            return Array.Empty<AssistantMessageSource>();
        }
    }
}
