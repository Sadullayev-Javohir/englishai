using Application.Assistant.Ports;

namespace Infrastructure.Assistant;

public sealed class NoOpLearningAssistant : ILearningAssistant
{
    public Task<string?> AnswerAsync(
        string question,
        IReadOnlyList<AssistantTurn> history,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
