using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Deterministic local stand-in for the LLM explain-chat. It produces no Uzbek text at all -
/// synthesizing an explanation is exactly the kind of dynamic content that must not be fabricated
/// without a real model (docs/development-guide.md rules 8, 11). So without an LLM key the panel simply stays in its
/// honest "unavailable" state. Replace with <see cref="LlmChatExplainer"/> when a key is configured.
/// </summary>
public sealed class LocalChatExplainer : IChatExplainer
{
    public Task<string?> ExplainAsync(
        string videoTitle,
        string fullTranscript,
        string focusText,
        string userMessage,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
