using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Speaking;
using Application.Speaking.Ports;
using Infrastructure.Common;
using Infrastructure.Llm;
using DomainSpeaking = Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// LLM-backed idea-card generator routed through the internal Hermes Agent Gateway. Given the live
/// conversation's topic, level and most recent turns, it returns a few concrete, answerable talking
/// points a stuck learner can use to keep going. Uses a budget model with a hard output-token cap, and
/// returns ONLY a strict JSON array of English learning content - never user-facing Uzbek prose (rule
/// 11). Every card is checked by <see cref="ContentLanguageGuard"/> before use, since the gateway's free
/// model can occasionally leak a non-English/non-Uzbek script. Anything unparseable or unclean falls
/// back to the universal scaffolds so the helper always yields something usable.
/// </summary>
public sealed class HermesIdeaCardGenerator : IIdeaCardGenerator
{
    private const int MaxCards = 4;
    private const int MaxOutputTokens = 220;
    private static readonly TimeSpan ReplyBudget = TimeSpan.FromMilliseconds(1800);
    private readonly ILlmCompletion _completion;
    private readonly LocalIdeaCardGenerator _fallback = new();

    public HermesIdeaCardGenerator(ILlmCompletion completion)
    {
        _completion = completion;
    }

    public async Task<IReadOnlyList<DomainSpeaking.IdeaCard>> GenerateAsync(
        DomainSpeaking.ConversationSession session,
        CancellationToken cancellationToken = default)
    {
        using var replyBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            var completionTask = _completion.CompleteAsync(
                InstructionPrompt, BuildRequest(session), MaxOutputTokens, replyBudget.Token);
            var timeoutTask = Task.Delay(ReplyBudget, cancellationToken);
            if (await Task.WhenAny(completionTask, timeoutTask) != completionTask)
            {
                replyBudget.Cancel();
                return await _fallback.GenerateAsync(session, cancellationToken);
            }

            var text = await completionTask;
            return string.IsNullOrWhiteSpace(text)
                ? await _fallback.GenerateAsync(session, cancellationToken)
                : await ParseOrDefaultAsync(text, session, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await _fallback.GenerateAsync(session, cancellationToken);
        }
    }

    private const string InstructionPrompt =
        """
        You help an Uzbek learner who is practising spoken English and has run out of things to say.
        Given the conversation's topic, the learner's CEFR level and the last few turns, produce a few
        "idea cards" that give the learner concrete, easy ways to keep talking. Each card has:
        - "prompt": one short, concrete, answerable talking-point question (NOT a broad open question).
          Prefer either/or or specific detail questions (who, when, where, how often, why, an example).
          Match the difficulty to the CEFR level: simpler for A1-A2, richer for B1+.
        - "starter": a short English sentence opener the learner can finish in their own words.
        - "emoji": a single emoji that fits the card.
        Write everything in English only.
        LANGUAGE RULE (MANDATORY): write ONLY in the Latin alphabet used by English. NEVER use Cyrillic
        or any other non-Latin script, and never switch to any other language.
        Keep the prompts tied to the current topic when there is one.

        Respond with ONLY a compact JSON array of exactly this shape and nothing else:
        [{"prompt":"...","starter":"...","emoji":"..."}]
        Give between 3 and 4 cards. Do not add any explanation, markdown, or extra text.
        """;

    private static string BuildRequest(DomainSpeaking.ConversationSession session)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Learner CEFR level: {session.Level}.");
        sb.AppendLine(string.IsNullOrWhiteSpace(session.Topic)
            ? "The conversation is an open free chat (no fixed topic)."
            : $"The conversation topic is: {session.Topic.Replace('_', ' ')}.");
        if (session.FocusWords.Count > 0)
            sb.AppendLine($"The learner is trying to use these words: {string.Join(", ", session.FocusWords)}.");

        // The last couple of turns anchor the cards to where the conversation actually is, so the
        // suggestions naturally continue the current thread rather than restarting the topic.
        var recent = session.Turns.TakeLast(4).ToList();
        if (recent.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Most recent turns:");
            foreach (var turn in recent)
            {
                var speaker = turn.Role == DomainSpeaking.ConversationRole.Learner ? "Learner" : "Partner";
                sb.AppendLine($"{speaker}: {turn.Text}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Give the idea cards now.");
        return sb.ToString();
    }

    private async Task<IReadOnlyList<DomainSpeaking.IdeaCard>> ParseOrDefaultAsync(
        string text,
        DomainSpeaking.ConversationSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            var start = text.IndexOf('[');
            var end = text.LastIndexOf(']');
            if (start < 0 || end <= start)
                return await LocalCardsAsync(session, cancellationToken);

            var json = text.Substring(start, end - start + 1);
            var parsed = JsonSerializer.Deserialize<List<CardDto>>(json);
            if (parsed is null)
                return await LocalCardsAsync(session, cancellationToken);

            var cards = parsed
                .Where(c => !string.IsNullOrWhiteSpace(c.Prompt) && !string.IsNullOrWhiteSpace(c.Starter))
                .Where(c => ContentLanguageGuard.IsClean(c.Prompt) && ContentLanguageGuard.IsClean(c.Starter))
                .Take(MaxCards)
                .Select(c => new DomainSpeaking.IdeaCard(
                    c.Prompt!.Trim(),
                    c.Starter!.Trim(),
                    string.IsNullOrWhiteSpace(c.Emoji) ? "💡" : c.Emoji!.Trim()))
                .ToList();

            return cards.Count == 0
                ? await LocalCardsAsync(session, cancellationToken)
                : cards;
        }
        catch (JsonException)
        {
            return await LocalCardsAsync(session, cancellationToken);
        }
    }

    private Task<IReadOnlyList<DomainSpeaking.IdeaCard>> LocalCardsAsync(
        DomainSpeaking.ConversationSession session,
        CancellationToken cancellationToken) =>
        _fallback.GenerateAsync(session, cancellationToken);

    private sealed record CardDto(
        [property: JsonPropertyName("prompt")] string? Prompt,
        [property: JsonPropertyName("starter")] string? Starter,
        [property: JsonPropertyName("emoji")] string? Emoji);
}
