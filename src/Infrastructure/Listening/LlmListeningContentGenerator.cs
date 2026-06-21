using System.Text.Json;
using Application.Listening.Models;
using Application.Listening.Ports;
using Domain.Assessment;
using Domain.Content;
using Infrastructure.Common;
using Infrastructure.Content;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Listening;

/// <summary>
/// LLM-backed listening-exercise generator (provider-agnostic via <see cref="ILlmCompletion"/> -
/// Gemini by default, rule 10). Given a topic title and
/// CEFR level it authors a short English transcript (the spoken script that Azure TTS synthesizes to
/// audio - rule 10) and comprehension questions with English explanations - the sanctioned dynamic
/// path of docs/development-guide.md rule 11 (the transcript, questions and explanations are the target language,
/// with structured, validated JSON, not free-invented Uzbek UI copy). Cost is controlled (rule 10)
/// by a budget model, a cached system prompt and a capped output. Any failure yields
/// <see cref="GeneratedListeningContent.Empty"/> so the exercise stays honestly "pending" (rules 8, 11).
/// </summary>
public sealed class LlmListeningContentGenerator : IListeningContentGenerator
{
    /// <summary>Output-token cap (docs/development-guide.md rule 10). Public so the batch backfill reuses it.</summary>
    public const int MaxOutputTokens = 2048;

    /// <summary>The cached system prompt. Public so the batch backfill reuses the identical prompt.</summary>
    public const string SystemPrompt =
        """
        You create listening-comprehension exercises for Uzbek learners of English.
        Given a TOPIC and a CEFR level, reply with ONLY a JSON object, no prose, no code fences:
        {"transcript":"<spoken script>",
         "questions":[{"q":"<question>","options":["<a>","<b>","<c>","<d>"],"answer":<index of correct option, 0-based>,"why":"<short English explanation>"}]}
        Rules:
        - "transcript": one natural, spoken-style English monologue or short dialogue about the topic,
          written at the given CEFR level (A1 simplest, C2 most advanced). A1/A2: 50-90 words;
          B1/B2: 90-150 words; C1/C2: 150-200 words. It will be read aloud by a text-to-speech voice,
          so write it to be heard, not read: clear sentences, no headings, no bullet points.
        - "questions": 3-4 comprehension questions answerable purely from hearing the transcript. Each
          has 2-4 options, a 0-based "answer" index, and "why": a short English explanation of why that
          answer is correct.
        - If TARGET WORDS are given, use as many as fit naturally in the transcript so the learner
          hears the topic's vocabulary in context - but never force a word where it does not belong.
        - Everything must be in English. Output valid JSON only.
        - LANGUAGE RULE (MANDATORY): All fields ("transcript", questions, options, "why") must be in
          English. The ONLY languages allowed are English and Uzbek. NEVER write Chinese, Japanese,
          Korean, Russian, Spanish, French, German, Arabic or ANY other language. NEVER use any Cyrillic
          or non-Latin script. NEVER add any text outside the JSON object (no commentary, no explanation,
          no code fences). Return ONLY the JSON object.
        """;

    /// <summary>
    /// Minimum acceptable comprehensibility score (docs/development-guide.md/PROJECT-SPEC i+1 principle). See
    /// <c>LlmReadingContentGenerator.MinComprehensibilityScore</c> for why the gate is lenient.
    /// </summary>
    private const double MinComprehensibilityScore = 55;

    private readonly ILlmCompletion _llm;
    private readonly IComprehensibilityScorer _scorer;
    private readonly ILogger<LlmListeningContentGenerator> _logger;

    public LlmListeningContentGenerator(
        ILlmCompletion llm, IComprehensibilityScorer scorer, ILogger<LlmListeningContentGenerator> logger)
    {
        _llm = llm;
        _scorer = scorer;
        _logger = logger;
    }

    public async Task<GeneratedListeningContent> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildUserPrompt(title, level, targetWords);
        // A failed call returns null, leaving the exercise pending rather than fabricating content
        // (docs/development-guide.md rules 8, 11).
        var text = await _llm.CompleteAsync(SystemPrompt, prompt, MaxOutputTokens, cancellationToken);
        var content = Parse(text);
        if (!content.HasContent)
            return content;

        var result = Score(content.Transcript, level, targetWords);
        if (result.Score >= MinComprehensibilityScore)
            return content;

        // The transcript leans too far outside the learner's known vocabulary for this CEFR level -
        // retry once with stronger guidance before accepting the content as-is (docs/development-guide.md rule 8:
        // never silently drop an exercise that already generated; a slightly-off transcript still
        // teaches more than a pending one).
        _logger.LogWarning(
            "Listening transcript for {Title} at {Level} scored {Score}% comprehensible (below {Min}%); retrying once.",
            title, level, result.Score, MinComprehensibilityScore);

        var retryPrompt = prompt +
            "\nIMPORTANT: Your previous attempt used too many rare/advanced words for this CEFR " +
            "level. Rewrite using simpler, more common everyday vocabulary appropriate to the level.";
        var retryText = await _llm.CompleteAsync(SystemPrompt, retryPrompt, MaxOutputTokens, cancellationToken);
        var retryContent = Parse(retryText);
        if (!retryContent.HasContent)
            return content;

        var retryResult = Score(retryContent.Transcript, level, targetWords);
        return retryResult.Score >= result.Score ? retryContent : content;
    }

    private ComprehensibilityResult Score(string transcript, CefrLevel level, IReadOnlyList<string>? targetWords) =>
        _scorer.Score(transcript, CoreWordLists.For(level), targetWords ?? Array.Empty<string>());

    /// <summary>
    /// Builds the per-topic user prompt, optionally listing the topic's vocabulary words to reuse.
    /// Public so the batch backfill builds the identical input.
    /// </summary>
    public static string BuildUserPrompt(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null) =>
        $"TOPIC: {title}\nCEFR LEVEL: {level}" + TargetWordPrompt.Line(targetWords);

    /// <summary>
    /// Parses the model's JSON reply into generated content. Tolerant of surrounding prose/fences
    /// (extracts the outermost JSON object); returns <see cref="GeneratedListeningContent.Empty"/> on
    /// any malformed or incomplete payload.
    /// </summary>
    public static GeneratedListeningContent Parse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return GeneratedListeningContent.Empty;

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            return GeneratedListeningContent.Empty;

        var json = response.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var transcript = StringProp(root, "transcript");
            if (string.IsNullOrWhiteSpace(transcript))
                return GeneratedListeningContent.Empty;
            if (!ContentLanguageGuard.IsClean(transcript))
                return GeneratedListeningContent.Empty;

            var questions = new List<GeneratedListeningQuestion>();
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
                    if (!ContentLanguageGuard.IsClean(q) || !ContentLanguageGuard.IsClean(why))
                        continue;

                    if (!entry.TryGetProperty("options", out var optionsEl) || optionsEl.ValueKind != JsonValueKind.Array)
                        continue;

                    var options = optionsEl.EnumerateArray()
                        .Where(o => o.ValueKind == JsonValueKind.String)
                        .Select(o => o.GetString()!.Trim())
                        .Where(o => o.Length > 0 && ContentLanguageGuard.IsClean(o))
                        .ToList();
                    if (options.Count < 2)
                        continue;

                    var answer = entry.TryGetProperty("answer", out var ansEl) && ansEl.ValueKind == JsonValueKind.Number
                        ? ansEl.GetInt32()
                        : 0;
                    if (answer < 0 || answer >= options.Count)
                        continue;

                    questions.Add(new GeneratedListeningQuestion(
                        q!.Trim(), options, answer, string.IsNullOrWhiteSpace(why) ? null : why!.Trim()));
                }
            }

            return questions.Count == 0
                ? GeneratedListeningContent.Empty
                : new GeneratedListeningContent(transcript!.Trim(), questions);
        }
        catch (JsonException)
        {
            return GeneratedListeningContent.Empty;
        }
    }

    private static string? StringProp(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
