using System.Text;
using Application.Video.Models;
using Application.Video.Ports;
using Infrastructure.Llm;

namespace Infrastructure.Video;

/// <summary>
/// LLM-backed transcript translator (provider-agnostic via <see cref="ILlmCompletion"/> - the free
/// Gemini model by default, rule 10; Claude is reserved for speaking/writing and is never wired here).
/// It translates a video's real English transcript into Uzbek line by line and extracts a glossary of
/// notable words - the sanctioned dynamic-Uzbek path of docs/development-guide.md rule 11 (translating an existing
/// source with structured, validated JSON output, not free-invented UI copy). Cost is controlled
/// (rule 10) by a budget model, a cached system prompt, bounded batches, and a capped output. Any
/// failure yields <see cref="TranscriptTranslation.Empty"/> so the transcript stays honestly untranslated.
/// </summary>
public sealed class LlmTranscriptTranslator : IVideoTranscriptTranslator
{
    // Lines per LLM call - bounds output tokens per request and keeps each batch cheap (rule 10).
    // Kept small because a line is now a full sentence (not a short caption fragment): a larger
    // batch's Uzbek output overran the token cap and truncated the JSON, losing the whole batch.
    private const int BatchSize = 15;
    private const int MaxOutputTokensPerBatch = 3072;

    private const string SystemPrompt =
        """
        You translate an English video transcript into Uzbek for Uzbek learners of English.
        You are given numbered English lines. Reply with ONLY a JSON object, no prose, no code fences:
        {"lines":["<Uzbek translation of line 1>", "..."], "glossary":[{"w":"<english word>","uz":"<short Uzbek meaning>"}]}
        Rules:
        - "lines" MUST have exactly one Uzbek translation per input line, in the same order.
        - Translations are natural, simple Uzbek (Latin script), faithful to the English meaning.
        - "glossary": pick the most useful/harder CONTENT words a learner may not know (skip
          common function words). Each: the base English word (lowercase) and a 1-3 word Uzbek meaning.
        - Output valid JSON only.
        """;

    private readonly ILlmCompletion _llm;

    public LlmTranscriptTranslator(ILlmCompletion llm) => _llm = llm;

    public async Task<TranscriptTranslation> TranslateAsync(
        IReadOnlyList<TranscriptLine> englishLines, CancellationToken cancellationToken = default)
    {
        if (englishLines.Count == 0)
            return TranscriptTranslation.Empty;

        var translations = new List<string?>(englishLines.Count);
        var glossary = new List<WordGloss>();
        var seenWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var offset = 0; offset < englishLines.Count; offset += BatchSize)
        {
            var batch = englishLines.Skip(offset).Take(BatchSize).ToList();
            var batchResult = await TranslateBatchAsync(batch, cancellationToken);

            // A failed/short batch contributes nulls so line indexes stay aligned (honest gaps).
            for (var i = 0; i < batch.Count; i++)
                translations.Add(i < batchResult.LineTranslations.Count ? batchResult.LineTranslations[i] : null);

            foreach (var entry in batchResult.Glossary)
                if (seenWords.Add(entry.Word))
                    glossary.Add(entry);
        }

        return new TranscriptTranslation(translations, glossary);
    }

    private async Task<TranscriptTranslation> TranslateBatchAsync(
        IReadOnlyList<TranscriptLine> batch, CancellationToken cancellationToken)
    {
        var numbered = new StringBuilder();
        for (var i = 0; i < batch.Count; i++)
            numbered.Append(i + 1).Append(". ").AppendLine(batch[i].EnglishText);

        // A failed call returns null, leaving those lines untranslated rather than failing the whole
        // transcript fill (docs/development-guide.md rules 8, 11).
        var text = await _llm.CompleteAsync(SystemPrompt, numbered.ToString(), MaxOutputTokensPerBatch, cancellationToken);
        return text is null
            ? TranscriptTranslation.Empty
            : TranscriptTranslationParser.Parse(text, batch.Count);
    }
}
