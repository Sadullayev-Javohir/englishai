using System.Text.Json;
using Application.Vocabulary.Models;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using Infrastructure.Common;
using Infrastructure.Llm;

namespace Infrastructure.Vocabulary;

/// <summary>
/// LLM-backed vocabulary passage generator (provider-agnostic via <see cref="ILlmCompletion"/> -
/// Gemini by default, rule 10). Given a topic title
/// and CEFR level it authors a short English passage and extracts the target words that appear in
/// it, each with a vetted Uzbek meaning and the exact example sentence from the passage - the
/// sanctioned dynamic path of docs/development-guide.md rule 11 (the passage is the target language; word meanings
/// are translations with structured, validated JSON, not free-invented UI copy). Cost is controlled
/// (rule 10) by a budget model, a cached system prompt and a capped output. Any failure yields
/// <see cref="GeneratedTopicContent.Empty"/> so the topic stays honestly "pending" (rules 8, 11).
/// </summary>
public sealed class LlmVocabularyPassageGenerator : IVocabularyPassageGenerator
{
    /// <summary>Output-token cap (docs/development-guide.md rule 10). Public so the batch backfill reuses it.</summary>
    public const int MaxOutputTokens = 2048;

    /// <summary>The cached system prompt. Public so the batch backfill reuses the identical prompt.</summary>
    public const string SystemPrompt =
        """
        You create vocabulary-in-context lessons for Uzbek learners of English.
        Given a TOPIC, a CEFR level and a CATEGORY PLAN, reply with ONLY a JSON object, no prose, no
        code fences:
        {"passage":"<text>","words":[{"w":"<word or chunk>","pos":"<category>","uz":"<short Uzbek meaning>","ex":"<sentence from the passage>"}]}
        Rules:
        - "passage": one cohesive English passage about the topic, written at the given CEFR level
          (A1 simplest, C2 most advanced). 110-180 words. Natural and engaging. Make it action-rich
          and descriptive (people doing things, qualities, manner) so verbs, adjectives and adverbs
          appear naturally - NOT just a list of nouns. At B1+ weave in the requested multi-word chunks
          (phrasal verbs, collocations, idioms, expressions) naturally inside real sentences.
        - "words": EXACTLY the requested number of the most useful target items, distributed to match
          the CATEGORY PLAN as closely as is natural (it tells you how many of each category to teach).
          Single words use the base/dictionary form (lowercase; verbs as the bare infinitive, e.g.
          "decide" not "decided"). Multi-word items (phrasal verbs, collocations, idioms, expressions)
          are written as the whole chunk (e.g. "give up", "make a decision", "break the ice"). Skip
          trivial function words unless the plan explicitly asks for prepositions/pronouns/determiners.
          Each item MUST actually appear in the passage (any inflected form of the head word counts).
        - "pos": the category, EXACTLY one of: "noun", "verb", "adjective", "adverb", "preposition",
          "conjunction", "pronoun", "determiner", "phrasal verb", "collocation", "idiom", "expression".
        - "uz": a 1-3 word natural Uzbek meaning (Latin script), faithful to how the item is used.
        - "ex": copy the exact sentence from the passage that contains the item (so it can be used
          for a fill-the-gap exercise). The sentence MUST contain the item.
        - Output valid JSON only.
        - LANGUAGE RULE (MANDATORY): All English fields ("passage", "w", "pos", "ex") must be in English.
          The "uz" field must be Latin-script Uzbek only (e.g. "o'", "g'", "sh", "ch", "ng"; never Cyrillic).
          The ONLY languages allowed are English and Uzbek. NEVER write Chinese, Japanese, Korean, Russian,
          Spanish, French, German, Arabic or ANY other language. NEVER use any Cyrillic or non-Latin script.
          NEVER add any text outside the JSON object (no commentary, no explanation, no code fences,
          no "Here is…"). Return ONLY the JSON object.
        """;

    private readonly ILlmCompletion _llm;

    public LlmVocabularyPassageGenerator(ILlmCompletion llm) => _llm = llm;

    public async Task<GeneratedTopicContent> GenerateAsync(
        string title, CefrLevel level, int targetWordCount, CancellationToken cancellationToken = default)
    {
        var prompt = BuildUserPrompt(title, level, targetWordCount);
        // A failed call returns null, leaving the topic pending rather than fabricating content
        // (docs/development-guide.md rules 8, 11).
        var text = await _llm.CompleteAsync(SystemPrompt, prompt, MaxOutputTokens, cancellationToken);
        return Parse(text);
    }

    /// <summary>
    /// Builds the per-topic user prompt, including the level-adaptive category plan. Public so the
    /// batch backfill builds the identical input (same prompt → cache hit, reproducible content).
    /// </summary>
    public static string BuildUserPrompt(string title, CefrLevel level, int targetWordCount) =>
        $"TOPIC: {title}\nCEFR LEVEL: {level}\nNUMBER OF WORDS: {targetWordCount}\n" +
        $"CATEGORY PLAN (aim for, in order of priority): {BuildCategoryPlan(level, targetWordCount)}";

    /// <summary>
    /// Renders the level-adaptive category quotas (<see cref="TopicWordPlan"/>) as a compact,
    /// model-readable list, e.g. "4 verb, 4 noun, 3 adjective, ...". Beginners get the single-word
    /// core; multi-word chunks are introduced from B1 up (level-adaptive, rule 11).
    /// </summary>
    public static string BuildCategoryPlan(CefrLevel level, int targetWordCount) =>
        string.Join(", ", TopicWordPlan.For(level, targetWordCount)
            .Select(q => $"{q.Count} {CategoryTag(q.Category)}"));

    // The exact tag string the model must echo back in "pos" for each category.
    private static string CategoryTag(PartOfSpeech category) => category switch
    {
        PartOfSpeech.PhrasalVerb => "phrasal verb",
        _ => category.ToString().ToLowerInvariant(),
    };

    /// <summary>
    /// Parses the model's JSON reply into generated content. Tolerant of surrounding prose/fences
    /// (extracts the outermost JSON object); returns <see cref="GeneratedTopicContent.Empty"/> on
    /// any malformed or incomplete payload.
    /// </summary>
    public static GeneratedTopicContent Parse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return GeneratedTopicContent.Empty;

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            return GeneratedTopicContent.Empty;

        var json = response.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("passage", out var passageEl) ||
                passageEl.ValueKind != JsonValueKind.String)
                return GeneratedTopicContent.Empty;

            var passage = passageEl.GetString();
            if (string.IsNullOrWhiteSpace(passage))
                return GeneratedTopicContent.Empty;
            // Reject any payload contaminated with non-target script (e.g. Chinese leaked by the model).
            if (!ContentLanguageGuard.IsClean(passage))
                return GeneratedTopicContent.Empty;

            var words = new List<GeneratedTopicWord>();
            if (root.TryGetProperty("words", out var wordsEl) && wordsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in wordsEl.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        continue;

                    var word = StringProp(entry, "w");
                    var uz = StringProp(entry, "uz");
                    var ex = StringProp(entry, "ex");
                    var pos = StringProp(entry, "pos");
                    if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(uz))
                        continue;
                    // Drop any entry whose English or Uzbek text carries a forbidden script.
                    if (!ContentLanguageGuard.IsClean(word) || !ContentLanguageGuard.IsClean(uz) ||
                        !ContentLanguageGuard.IsClean(ex) || !ContentLanguageGuard.IsClean(pos))
                        continue;

                    words.Add(new GeneratedTopicWord(word!.Trim(), uz!.Trim(),
                        string.IsNullOrWhiteSpace(ex) ? null : ex!.Trim(),
                        string.IsNullOrWhiteSpace(pos) ? null : pos!.Trim()));
                }
            }

            return words.Count == 0
                ? GeneratedTopicContent.Empty
                : new GeneratedTopicContent(passage!.Trim(), words);
        }
        catch (JsonException)
        {
            return GeneratedTopicContent.Empty;
        }
    }

    private static string? StringProp(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
