using Application.Assistant.Ports;

namespace Infrastructure.Assistant;

public sealed class NoOpProjectAssistant : IProjectAssistant
{
    public Task<string?> AnswerAsync(
        string question,
        IReadOnlyList<AssistantTurn> history,
        string locale = "uz",
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
