using System.Text;
using Application.Video.Ports;
using Infrastructure.Common;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Video;

/// <summary>
/// LLM-backed explain-chat (provider-agnostic via <see cref="ILlmCompletion"/>, rule 10). Answers a
/// learner's question about a real piece of lesson text in Uzbek - the sanctioned dynamic-Uzbek path
/// of docs/development-guide.md rule 11, wider than translation (it synthesizes an explanation, not just translates
/// shown text) but kept narrow the same way: a fixed system prompt constrains the model to the given
/// source text only, plain Uzbek, no markdown/English/chit-chat, and a capped output. Any failure
/// yields <c>null</c> so the UI shows an honest "javob olinmadi" state rather than fabricated text.
/// </summary>
public sealed class LlmChatExplainer : IChatExplainer
{
    private const int MaxOutputTokens = 220;

    private const string SystemPrompt =
        """
        You are an English-teaching assistant for an Uzbek learner, embedded in a video lesson page.
        You are given a real video's title, its complete English transcript (FULL_TRANSCRIPT), the
        currently visible line when available (CURRENT_FOCUS), the learner's question (QUESTION), and
        optionally a few prior turns of the same chat (HISTORY).
        Reply with ONLY the answer as plain, natural Uzbek text written with ASCII Latin letters - no
        markdown, no headings, no quotes around your answer, no "Javob:" label. English is allowed ONLY
        when quoting or explaining an English word, phrase, grammar form, or example from the lesson.
        Never use a third language, Cyrillic, Arabic, CJK, Hangul, Kana, accented Latin letters, emojis,
        typographic quotes/dashes, or any other non-ASCII character. Write Uzbek apostrophes as plain '.
        Rules:
        - For questions about the video's topic, meaning, events, arguments, or summary, use the whole
          FULL_TRANSCRIPT, not only CURRENT_FOCUS.
        - For questions about a word, phrase, grammar point, or current sentence, prioritize CURRENT_FOCUS
          while using FULL_TRANSCRIPT when wider context is needed.
        - Answer strictly from VIDEO_TITLE, FULL_TRANSCRIPT, CURRENT_FOCUS, and QUESTION; do not drift.
        - If asked why a word/phrase is used, explain its meaning and role in that sentence simply.
        - If asked to break down a phrase or collocation, split it into its parts and explain each
          briefly, in plain Uzbek a beginner-to-intermediate learner can follow.
        - Keep the answer concise (a few short sentences), not an essay.
        - Never invent facts about the source that aren't there.
        """;

    private readonly ILlmCompletion _llm;
    private readonly ILogger _logger;

    public LlmChatExplainer(ILlmCompletion llm, ILogger<LlmChatExplainer>? logger = null)
    {
        _llm = llm;
        _logger = logger ?? (ILogger)NullLogger<LlmChatExplainer>.Instance;
    }

    public async Task<string?> ExplainAsync(
        string videoTitle,
        string fullTranscript,
        string focusText,
        string userMessage,
        IReadOnlyList<ChatTurn> history,
        CancellationToken cancellationToken = default)
    {
        var prompt = new StringBuilder();
        prompt.Append("VIDEO_TITLE: ").AppendLine(videoTitle);
        prompt.AppendLine("FULL_TRANSCRIPT:").AppendLine(fullTranscript);
        prompt.Append("CURRENT_FOCUS: ").AppendLine(focusText);
        if (history.Count > 0)
        {
            prompt.AppendLine("HISTORY:");
            foreach (var turn in history)
                prompt.Append(turn.Role).Append(": ").AppendLine(turn.Text);
        }
        prompt.Append("QUESTION: ").Append(userMessage);

        // A failed call returns null, leaving the UI to show an honest "unavailable" state rather
        // than fabricated text (docs/development-guide.md rules 8, 11).
        var raw = await _llm.CompleteAsync(SystemPrompt, prompt.ToString(), MaxOutputTokens, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Fold typographic look-alikes (curly quotes, the Uzbek okina, em dashes) to their ASCII
        // equivalents BEFORE the guard runs, so a cosmetic character does not cost the learner an
        // otherwise-good answer. Genuine script contamination survives normalization and is still
        // rejected below (rule 11).
        var text = ContentLanguageGuard.NormalizeToAscii(raw)?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!ContentLanguageGuard.IsClean(text))
        {
            // Without this the rejection was completely silent: the provider chain logged a success,
            // the admin panel showed no error, and the learner still saw "javob bera olmadi". Log the
            // offending characters (not the whole reply - it can be long) so the cause is visible.
            _logger.LogWarning(
                "Video explain reply rejected by the language guard: it contains non-target characters " +
                "({Offenders}). Answering as unavailable rather than showing contaminated text.",
                DescribeOffenders(text));
            return null;
        }

        return text;
    }

    /// <summary>
    /// Renders the distinct out-of-range characters of a rejected reply as "'x' (U+00E9)" pairs, capped
    /// so one badly-contaminated reply cannot flood the log.
    /// </summary>
    private static string DescribeOffenders(string text)
    {
        const int maxReported = 8;
        var offenders = text.Where(ch => ch > 127).Distinct().Take(maxReported).ToArray();
        return offenders.Length == 0
            ? "none"
            : string.Join(", ", offenders.Select(ch => $"'{ch}' (U+{(int)ch:X4})"));
    }
}
