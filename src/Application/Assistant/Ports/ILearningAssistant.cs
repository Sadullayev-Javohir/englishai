namespace Application.Assistant.Ports;

public sealed record AssistantTurn(string Role, string Text);

public interface ILearningAssistant
{
    Task<string?> AnswerAsync(
        string question,
        IReadOnlyList<AssistantTurn> history,
        CancellationToken cancellationToken = default);
}
