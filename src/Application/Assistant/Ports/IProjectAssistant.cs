namespace Application.Assistant.Ports;

public interface IProjectAssistant
{
    Task<string?> AnswerAsync(
        string question,
        IReadOnlyList<AssistantTurn> history,
        string locale = "uz",
        CancellationToken cancellationToken = default);
}
