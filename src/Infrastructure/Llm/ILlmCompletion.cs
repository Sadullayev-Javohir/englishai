namespace Infrastructure.Llm;

/// <summary>
/// A single-shot text completion behind a provider-agnostic port. Every content generator and
/// translator reduces its LLM call to "(system prompt, user prompt, output cap) → reply text", so the
/// concrete provider (Anthropic Claude, Google Gemini, …) is a DI choice rather than baked into each
/// adapter. This keeps each module's prompt + parser single-sourced (docs/development-guide.md rule 9) while letting
/// the cheap bulk modules run on a free model and the quality-critical ones (speaking, writing) stay
/// on Claude (rule 10 cost control). Implementations are best-effort: a failed call returns
/// <c>null</c> so the caller leaves its content honestly "pending"/"unavailable" (rules 8, 11).
/// </summary>
public interface ILlmCompletion
{
    /// <summary>The model id this completion is wired to (e.g. <c>gemini-2.5-flash</c>,
    /// <c>claude-haiku-4-5</c>). Used by the backfill router to send each request to the matching
    /// provider.</summary>
    string Model { get; }

    Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        CancellationToken cancellationToken = default);
}

public interface IConversationLlmCompletion : ILlmCompletion
{
    Task<string?> CompleteConversationAsync(
        string systemPrompt,
        IReadOnlyList<(bool IsUser, string Text)> turns,
        int maxOutputTokens,
        CancellationToken cancellationToken = default);
}
