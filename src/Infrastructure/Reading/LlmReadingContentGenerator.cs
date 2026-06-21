using System.Text.Json;
using Application.Reading.Models;
using Application.Reading.Ports;
using Domain.Assessment;
using Domain.Content;
using Infrastructure.Common;
using Infrastructure.Content;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Reading;

/// <summary>
/// LLM-backed reading-lesson generator (provider-agnostic via <see cref="ILlmCompletion"/> - Gemini
/// by default, rule 10). Given a topic title and
/// CEFR level it authors a short English passage, an interactive glossary (the Uzbek support words
/// - richer at A1–A2, sparser by B1+) and comprehension questions with English explanations - the
/// sanctioned dynamic path of docs/development-guide.md rule 11 (the passage, questions and explanations are the
/// target language; glossary meanings are translations with structured, validated JSON, not
/// free-invented UI copy). Cost is controlled (rule 10) by a budget model, a cached system prompt
/// and a capped output. Any failure yields <see cref="GeneratedReadingContent.Empty"/> so the
/// lesson stays honestly "pending" (rules 8, 11).
/// </summary>
public sealed class LlmReadingContentGenerator : IReadingContentGenerator
{
    /// <summary>Output-token cap (docs/development-guide.md rule 10). Public so the batch backfill reuses it.</summary>
    public const int MaxOutputTokens = 3072;

    /// <summary>The cached system prompt. Public so the batch backfill reuses the identical prompt.</summary>
    public const string SystemPrompt =
        """
        You create reading-comprehension lessons for Uzbek learners of English.
        Given a TOPIC and a CEFR level, reply with ONLY a JSON object, no prose, no code fences:
        {"body":"<passage>","glossary":[{"w":"<word>","uz":"<short Uzbek meaning>","ex":"<sentence from the passage>"}],
         "questions":[{"q":"<question>","options":["<a>","<b>","<c>","<d>"],"answer":<index of correct option, 0-based>,"why":"<short English explanation>"}]}
        Rules:
        - "body": one cohesive, engaging English passage about the topic, written at the given CEFR
          level (A1 simplest, C2 most advanced). A1/A2: 90-150 words; B1/B2: 150-220 words; C1/C2: 200-260 words.
        - "glossary": the most useful words a learner at this level should learn from the passage.
          Give MORE words at A1/A2 (10-12) as Uzbek support, FEWER at B1+ (5-8). Each word MUST appear
          in the passage. "uz" is a 1-3 word natural Uzbek meaning (Latin script). "ex" copies the
          exact sentence from the passage that contains the word.
        - "questions": 3-5 comprehension questions answerable from the passage. Each has 2-4 options,
          a 0-based "answer" index, and "why": a short English explanation of why that answer is correct.
        - If TARGET WORDS are given, weave as many as fit naturally into the passage (and prefer them
          in the glossary) so the learner meets the topic's vocabulary again - but never force a word
          where it does not belong.
        - Everything except the Uzbek glossary meanings must be in English. Output valid JSON only.
        - LANGUAGE RULE (MANDATORY): All English fields ("body", questions, options, "why") must be in
          English. The "uz" glossary field must be Latin-script Uzbek only (never Cyrillic).
          The ONLY languages allowed are English and Uzbek. NEVER write Chinese, Japanese, Korean,
          Russian, Spanish, French, German, Arabic or ANY other language. NEVER use any Cyrillic or
          non-Latin script. NEVER add any text outside the JSON object (no commentary, no explanation,
          no code fences). Return ONLY the JSON object.
        """;

    /// <summary>
    /// Minimum acceptable comprehensibility score (docs/development-guide.md/PROJECT-SPEC i+1 principle: a passage
    /// should be mostly made of words the learner already knows, plus the topic's own new words).
    /// The known-word baseline (<see cref="Content.CoreWordLists"/>) is a coarse proxy, not an
    /// exact learner vocabulary, so the gate stays lenient - it exists to catch passages that
    /// drift far outside the target level, not to police every borderline word.
    /// </summary>
    private const double MinComprehensibilityScore = 55;

    private readonly ILlmCompletion _llm;
    private readonly IComprehensibilityScorer _scorer;
    private readonly ILogger<LlmReadingContentGenerator> _logger;

    public LlmReadingContentGenerator(
        ILlmCompletion llm, IComprehensibilityScorer scorer, ILogger<LlmReadingContentGenerator> logger)
    {
        _llm = llm;
        _scorer = scorer;
        _logger = logger;
    }

    public async Task<GeneratedReadingContent> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildUserPrompt(title, level, targetWords);
        // A failed call returns null, leaving the lesson pending rather than fabricating content
        // (docs/development-guide.md rules 8, 11).
        var text = await _llm.CompleteAsync(SystemPrompt, prompt, MaxOutputTokens, cancellationToken);
        var content = Parse(text);
        if (!content.HasContent)
            return content;

        var result = Score(content.Body, level, targetWords);
        if (result.Score >= MinComprehensibilityScore)
            return content;

        // The passage leans too far outside the learner's known vocabulary for this CEFR level -
        // retry once with stronger guidance before accepting the content as-is (docs/development-guide.md rule 8:
        // never silently drop a lesson that already generated; a slightly-off passage still teaches
        // more than a pending one).
        _logger.LogWarning(
            "Reading passage for {Title} at {Level} scored {Score}% comprehensible (below {Min}%); retrying once.",
            title, level, result.Score, MinComprehensibilityScore);

        var retryPrompt = prompt +
            "\nIMPORTANT: Your previous attempt used too many rare/advanced words for this CEFR " +
            "level. Rewrite using simpler, more common everyday vocabulary appropriate to the level.";
        var retryText = await _llm.CompleteAsync(SystemPrompt, retryPrompt, MaxOutputTokens, cancellationToken);
        var retryContent = Parse(retryText);
        if (!retryContent.HasContent)
            return content;

        var retryResult = Score(retryContent.Body, level, targetWords);
        return retryResult.Score >= result.Score ? retryContent : content;
    }

    private ComprehensibilityResult Score(string body, CefrLevel level, IReadOnlyList<string>? targetWords) =>
        _scorer.Score(body, CoreWordLists.For(level), targetWords ?? Array.Empty<string>());

    /// <summary>
    /// Builds the per-topic user prompt, optionally listing the topic's vocabulary words to reuse.
    /// Public so the batch backfill builds the identical input.
    /// </summary>
    public static string BuildUserPrompt(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null) =>
        $"TOPIC: {title}\nCEFR LEVEL: {level}" + TargetWordPrompt.Line(targetWords);

    /// <summary>
    /// Parses the model's JSON reply into generated content. Tolerant of surrounding prose/fences
    /// (extracts the outermost JSON object); returns <see cref="GeneratedReadingContent.Empty"/> on
    /// any malformed or incomplete payload.
    /// </summary>
    public static GeneratedReadingContent Parse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return GeneratedReadingContent.Empty;

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            return GeneratedReadingContent.Empty;

        var json = response.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var body = StringProp(root, "body");
            if (string.IsNullOrWhiteSpace(body))
                return GeneratedReadingContent.Empty;
            if (!ContentLanguageGuard.IsClean(body))
                return GeneratedReadingContent.Empty;

            var glossary = new List<GeneratedReadingGlossary>();
            if (root.TryGetProperty("glossary", out var glossaryEl) && glossaryEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in glossaryEl.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        continue;

                    var word = StringProp(entry, "w");
                    var uz = StringProp(entry, "uz");
                    var ex = StringProp(entry, "ex");
                    if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(uz))
                        continue;
                    if (!ContentLanguageGuard.IsClean(word) || !ContentLanguageGuard.IsClean(uz) ||
                        !ContentLanguageGuard.IsClean(ex))
                        continue;

                    glossary.Add(new GeneratedReadingGlossary(
                        word!.Trim(), uz!.Trim(), string.IsNullOrWhiteSpace(ex) ? null : ex!.Trim()));
                }
            }

            var questions = new List<GeneratedReadingQuestion>();
            if (root.TryGetProperty("questions", out var questionsEl) && questionsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in questionsEl.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        continue;

                    var q = StringProp(entry, "q");
                    var why = StringProp(entry, "why");
                    if (string.IsNullOrWhiteSpace(q))
                        continue;

                    if (!entry.TryGetProperty("options", out var optionsEl) || optionsEl.ValueKind != JsonValueKind.Array)
                        continue;

                    var options = optionsEl.EnumerateArray()
                        .Where(o => o.ValueKind == JsonValueKind.String)
                        .Select(o => o.GetString()!.Trim())
                        .Where(o => o.Length > 0)
                        .ToList();
                    if (options.Count < 2)
                        continue;

                    var answer = entry.TryGetProperty("answer", out var ansEl) && ansEl.ValueKind == JsonValueKind.Number
                        ? ansEl.GetInt32()
                        : 0;
                    if (answer < 0 || answer >= options.Count)
                        continue;

                    questions.Add(new GeneratedReadingQuestion(
                        q!.Trim(), options, answer, string.IsNullOrWhiteSpace(why) ? null : why!.Trim()));
                }
            }

            return questions.Count == 0
                ? GeneratedReadingContent.Empty
                : new GeneratedReadingContent(body!.Trim(), glossary, questions);
        }
        catch (JsonException)
        {
            return GeneratedReadingContent.Empty;
        }
    }

    private static string? StringProp(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
