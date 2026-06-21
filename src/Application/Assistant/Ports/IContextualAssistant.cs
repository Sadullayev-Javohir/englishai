namespace Application.Assistant.Ports;

public sealed record ContextualAssistantTurn(string Role, string Text);

public interface IContextualAssistant
{
    Task<string?> AnswerAsync(
        string area,
        string title,
        string context,
        string focusText,
        string question,
        IReadOnlyList<ContextualAssistantTurn> history,
        CancellationToken cancellationToken = default);
}
