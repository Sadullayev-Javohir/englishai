using Microsoft.Extensions.Logging;

namespace Infrastructure.Llm;

/// <summary>
/// Tries <paramref name="primary"/> and falls back to <paramref name="fallback"/> when it returns
/// nothing. The pair is deliberately untyped so the same class expresses both orders: SOL-then-Hermes
/// for quality-critical features, and Hermes-then-SOL for the high-volume budget path, where the
/// self-hosted gateway is preferred but must not become a single point of failure
/// (see <see cref="AiRoutingOptions"/>).
/// </summary>
public sealed class PrimaryFallbackLlmCompletion(
    IConversationLlmCompletion primary,
    IConversationLlmCompletion fallback,
    ILogger<PrimaryFallbackLlmCompletion> logger) : IConversationLlmCompletion
{
    public string Model => primary.Model;

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        CancellationToken cancellationToken = default)
    {
        var result = await primary.CompleteAsync(systemPrompt, userPrompt, maxOutputTokens, cancellationToken);
        if (!string.IsNullOrWhiteSpace(result)) return result;
        LogFallback();
        return await fallback.CompleteAsync(systemPrompt, userPrompt, maxOutputTokens, cancellationToken);
    }

    public async Task<string?> CompleteConversationAsync(
        string systemPrompt,
        IReadOnlyList<(bool IsUser, string Text)> turns,
        int maxOutputTokens,
        CancellationToken cancellationToken = default)
    {
        var result = await primary.CompleteConversationAsync(systemPrompt, turns, maxOutputTokens, cancellationToken);
        if (!string.IsNullOrWhiteSpace(result)) return result;
        LogFallback();
        return await fallback.CompleteConversationAsync(systemPrompt, turns, maxOutputTokens, cancellationToken);
    }

    private void LogFallback() =>
        logger.LogInformation(
            "LLM {Primary} returned nothing; falling back to {Fallback}.", primary.Model, fallback.Model);
}
