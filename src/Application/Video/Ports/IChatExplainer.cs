namespace Application.Video.Ports;

/// <summary>One prior turn of an explain-chat exchange, sent back as short context for a follow-up.</summary>
public sealed record ChatTurn(string Role, string Text);

/// <summary>
/// Answers a learner's free-text question about a real video transcript in Uzbek. The full transcript
/// supports video-level comprehension questions, while a current focus line supports word/phrase
/// explanations. This is a wider dynamic-Uzbek exception than <see cref="IVideoTranscriptTranslator"/>'s
/// (it synthesizes a new explanation, not just a translation of shown text), sanctioned narrowly under
/// docs/development-guide.md rule 11 the same way: the model is shown only real lesson content and a bounded, capped
/// system prompt, never invents its own topic. Implementations must return <c>null</c> (never throw,
/// never fabricate) when no explainer is configured or a call fails, so the UI can show an honest
/// "javob olinmadi" state rather than fake text (rules 8, 11).
/// </summary>
public interface IChatExplainer
{
    /// <summary>
    /// A plain-Uzbek explanation answering <paramref name="userMessage"/> from the real lesson
    /// <paramref name="fullTranscript"/>, using <paramref name="focusText"/> for a current-line question,
    /// or <c>null</c> when unavailable. <paramref name="history"/> is the trailing conversation context.
    /// </summary>
    Task<string?> ExplainAsync(
        string videoTitle,
        string fullTranscript,
        string focusText,
        string userMessage,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default);
}
