namespace Application.Assistant.Ports;

public sealed record AssistantKnowledgeRequest(
    Guid LearnerId,
    string Area,
    string ResourceType,
    string? ResourceId,
    string Title,
    string Question,
    string PageContext,
    string FocusText);

public sealed record AssistantKnowledgeSource(
    string Area,
    string ResourceType,
    string ResourceId,
    string Title,
    string Route,
    string Content,
    double Relevance);

public sealed record AssistantKnowledgeResult(
    string Context,
    IReadOnlyList<AssistantKnowledgeSource> Sources)
{
    public static AssistantKnowledgeResult Empty { get; } = new(string.Empty, Array.Empty<AssistantKnowledgeSource>());
}

public interface IAssistantKnowledgeRetriever
{
    Task<AssistantKnowledgeResult> RetrieveAsync(
        AssistantKnowledgeRequest request,
        CancellationToken cancellationToken = default);
}
