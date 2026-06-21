using Application.Translation.Ports;
using Domain.Assessment;
using Infrastructure.Llm;

namespace Infrastructure.Translation;

/// <summary>
/// LLM-backed sentence translator (provider-agnostic via <see cref="ILlmCompletion"/> - Gemini's free
/// tier by default, rule 10). It translates one piece of real English teaching text into natural
/// Uzbek for an Uzbek learner - the sanctioned dynamic-Uzbek path of docs/development-guide.md rule 11 (translating an
/// existing source it is shown, not inventing UI copy). Results are cached above this adapter
/// (<see cref="ITranslationCache"/>) so each sentence is paid for once. Any failure yields <c>null</c>
/// so the UI stays honestly "unavailable" (rules 8, 11).
/// </summary>
public sealed class LlmTextTranslator : ITextTranslator
{
    private const int MaxOutputTokens = 512;

    private const string SystemPrompt =
        """
        You translate one short piece of English (a sentence or short paragraph from an English
        lesson) into Uzbek for an Uzbek learner of English. Reply with ONLY the Uzbek translation as
        plain text - no quotes, no English, no notes, no explanation, no labels.
        Rules:
        - Translate faithfully and naturally into modern Uzbek (Latin script).
        - Keep it a clear, accurate translation of the meaning; do not add or omit information.
        - If a CEFR level is given, keep the Uzbek wording simple enough for that level.
        """;

    private readonly ILlmCompletion _llm;

    public LlmTextTranslator(ILlmCompletion llm) => _llm = llm;

    public async Task<string?> TranslateAsync(
        string englishText, CefrLevel? level = null, TranslationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            return null;

        var prompt = $"""
        CEFR LEVEL: {level?.ToString() ?? "unknown"}
        SPEAKER: {context?.Speaker ?? "unknown"}
        TOPIC: {context?.Topic ?? "unknown"}
        PREVIOUS TURNS: {string.Join(" | ", context?.PreviousTurns ?? Array.Empty<string>())}
        TRANSLATE ONLY THIS ENGLISH TEXT: {englishText}
        """;

        // A failed call returns null, leaving the sentence untranslated rather than fabricating text
        // (docs/development-guide.md rules 8, 11).
        var text = (await _llm.CompleteAsync(SystemPrompt, prompt, MaxOutputTokens, cancellationToken))?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
