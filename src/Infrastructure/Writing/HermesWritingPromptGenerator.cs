using System.Text.Json;
using Application.Writing.Content;
using Application.Writing.Models;
using Application.Writing.Ports;
using Domain.Assessment;
using Infrastructure.Common;
using Infrastructure.Llm;

namespace Infrastructure.Writing;

/// <summary>
/// LLM-backed writing-prompt generator routed through the internal Hermes Agent Gateway. Given a topic
/// title and CEFR level it authors a concrete English writing prompt about the topic plus a few short
/// English guidance hints - the sanctioned dynamic path of docs/development-guide.md rule 11 (the prompt and hints are
/// the target language, structured and validated JSON, not free-invented Uzbek UI copy). Cost is
/// controlled (rule 10) by a budget model and a capped output. Transport and admission failures
/// propagate so learner-facing requests can show an actionable retry state.
/// </summary>
public sealed class HermesWritingPromptGenerator : IWritingPromptGenerator
{
    /// <summary>Output-token cap (docs/development-guide.md rule 10). Public so the batch backfill reuses it.</summary>
    public const int MaxOutputTokens = 800;

    /// <summary>The system prompt. Public so the batch backfill reuses the identical prompt.</summary>
    public const string SystemPrompt =
        """
        You create writing tasks for Uzbek learners of English.
        Given a TOPIC, a CEFR level and a GENRE (text type), reply with ONLY a JSON object, no prose,
        no code fences:
        {"prompt":"<the writing task>","guidance":["<hint>","<hint>"]}
        Rules:
        - "prompt": one concrete, engaging English writing task about the TOPIC, written in the given
          GENRE (e.g. an email, a postcard, a review, a report, an opinion essay) and pitched at the
          given CEFR level. Make it specific to the topic, not generic. Do NOT state a word count.
        - "guidance": 2-5 short English hints telling the learner what to include (structure, content,
          useful language) for this genre. Give MORE hints at A1/A2 as support, FEWER at C1+. Each
          hint is one short sentence.
        - If TARGET WORDS are given, encourage the learner to use them: name a few of them in the
          prompt or guidance as words to include, so they practise the topic's vocabulary in writing.
        - Everything must be in English. Output valid JSON only.
        - LANGUAGE RULE (MANDATORY): The "prompt" and all "guidance" entries must be in English. The
          ONLY languages allowed are English and Uzbek. NEVER write Chinese, Japanese, Korean, Russian,
          Spanish, French, German, Arabic or ANY other language. NEVER use any Cyrillic or non-Latin
          script. NEVER add any text outside the JSON object (no commentary, no explanation, no code
          fences). Return ONLY the JSON.
        """;

    private readonly ILlmCompletion _completion;

    public HermesWritingPromptGenerator(ILlmCompletion completion)
    {
        _completion = completion;
    }

    public async Task<GeneratedWritingPrompt> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default)
    {
        var text = await _completion.CompleteAsync(
            SystemPrompt, BuildUserPrompt(title, level, targetWords), MaxOutputTokens, cancellationToken);
        return Parse(text);
    }

    /// <summary>
    /// Builds the per-topic user prompt (the genre is deterministic for a title+level, so the batch
    /// backfill produces the identical input). Public so the batch backfill reuses it.
    /// </summary>
    public static string BuildUserPrompt(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null)
    {
        var genre = WritingGenreCatalog.ForTopic(title, level);
        return $"TOPIC: {title}\nCEFR LEVEL: {level}\nGENRE: {genre.Name}"
            + TargetWordPrompt.Line(targetWords);
    }

    /// <summary>
    /// Parses the model's JSON reply into a generated prompt. Tolerant of surrounding prose/fences
    /// (extracts the outermost JSON object); returns <see cref="GeneratedWritingPrompt.Empty"/> on
    /// any malformed or incomplete payload.
    /// </summary>
    public static GeneratedWritingPrompt Parse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return GeneratedWritingPrompt.Empty;

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            return GeneratedWritingPrompt.Empty;

        var json = response.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var prompt = StringProp(root, "prompt");
            if (string.IsNullOrWhiteSpace(prompt))
                return GeneratedWritingPrompt.Empty;
            if (!ContentLanguageGuard.IsClean(prompt))
                return GeneratedWritingPrompt.Empty;

            var guidance = new List<string>();
            if (root.TryGetProperty("guidance", out var guidanceEl) && guidanceEl.ValueKind == JsonValueKind.Array)
            {
                guidance.AddRange(guidanceEl.EnumerateArray()
                    .Where(g => g.ValueKind == JsonValueKind.String)
                    .Select(g => g.GetString()!.Trim())
                    .Where(g => g.Length > 0 && ContentLanguageGuard.IsClean(g)));
            }

            return new GeneratedWritingPrompt(prompt!.Trim(), guidance);
        }
        catch (JsonException)
        {
            return GeneratedWritingPrompt.Empty;
        }
    }

    private static string? StringProp(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
