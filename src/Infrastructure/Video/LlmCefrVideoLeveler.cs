using Application.Video.Ports;
using Domain.Assessment;
using Infrastructure.Llm;

namespace Infrastructure.Video;

/// <summary>
/// LLM-backed CEFR video leveler (provider-agnostic via <see cref="ILlmCompletion"/> - the free Gemini
/// model by default, rule 10; Claude is reserved for speaking/writing and is never wired here). The
/// model reads the transcript and returns a single CEFR code (docs/development-guide.md rule 11 - structured output,
/// no free Uzbek text); a budget model, a tiny output-token cap and a cached system prompt keep cost
/// minimal (rule 10).
/// </summary>
public sealed class LlmCefrVideoLeveler : ICefrVideoLeveler
{
    /// <summary>Output-token cap - a CEFR code is a couple of tokens (docs/development-guide.md rule 10).</summary>
    public const int MaxOutputTokens = 8;

    public const string SystemPrompt =
        """
        You are a CEFR leveling assistant. Given an English video transcript, estimate the
        single CEFR level a learner needs to comfortably understand it. Reply with ONLY one
        of these exact codes and nothing else: A1, A2, B1, B2, C1, C2.
        """;

    private readonly ILlmCompletion _llm;

    public LlmCefrVideoLeveler(ILlmCompletion llm) => _llm = llm;

    public async Task<CefrLevel> EstimateLevelAsync(string transcript, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return CefrLevel.A2;

        var text = await _llm.CompleteAsync(SystemPrompt, transcript, MaxOutputTokens, cancellationToken);
        return ParseLevel(text);
    }

    public static CefrLevel ParseLevel(string? text)
    {
        var token = new string((text ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        return token switch
        {
            _ when token.Contains("A1") => CefrLevel.A1,
            _ when token.Contains("A2") => CefrLevel.A2,
            _ when token.Contains("B1") => CefrLevel.B1,
            _ when token.Contains("B2") => CefrLevel.B2,
            _ when token.Contains("C1") => CefrLevel.C1,
            _ when token.Contains("C2") => CefrLevel.C2,
            _ => CefrLevel.B1
        };
    }
}
