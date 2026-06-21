namespace Application.Assistant.Ports;

public sealed record ContextualAssistantWork(
    string CacheKey,
    string Area,
    string Title,
    string Context,
    string FocusText,
    string Question,
    IReadOnlyList<ContextualAssistantTurn> History);

public interface IContextualAssistantCoordinator
{
    Task<string?> ExecuteAsync(ContextualAssistantWork work, string callerKey, CancellationToken cancellationToken);
}
