using System.Text.Json;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using Infrastructure.Common;
using Infrastructure.Llm;

namespace Infrastructure.Grammar;

/// <summary>
/// LLM-backed grammar-lesson generator (provider-agnostic via <see cref="ILlmCompletion"/> - Gemini
/// by default, rule 10). Given a topic title, a
/// grammar focus and a CEFR level it authors a 5-step lesson teaching that grammar point in the
/// topic's own context: a contextual intro, an English rule explanation, recognition + active-use
/// exercises (each with an English explanation) and Speaking/Writing application tasks - the
/// sanctioned dynamic path of docs/development-guide.md rule 11 (everything is the target language, structured and
/// validated). Cost is controlled (rule 10) by a budget model, a cached system prompt and a capped
/// output. Any failure yields <see cref="GeneratedGrammarContent.Empty"/> so the lesson stays
/// honestly "pending" (rules 8, 11). The "mistakes" step is NEVER free Uzbek prose from the model:
/// the LLM returns one of the fixed <see cref="MistakeCodes"/>, resolved to vetted Uzbek text via
/// <see cref="IGrammarContentProvider.GetMistakeExplanation"/> - the same code-then-lookup pattern
/// the Writing module uses for its issue explanations (rule 11).
/// </summary>
public sealed class LlmGrammarContentGenerator : IGrammarContentGenerator
{
    /// <summary>Output-token cap (docs/development-guide.md rule 10). Public so the batch backfill reuses it.</summary>
    public const int MaxOutputTokens = 4096;

    /// <summary>
    /// The fixed taxonomy of common-mistake codes the LLM may choose from for "mistakes" (rule 11 -
    /// never free Uzbek prose). Each has a vetted Uzbek explanation in
    /// <c>Resources/uz/grammar-content.json</c> under the key <c>grammar.mistake.&lt;code&gt;</c>.
    /// </summary>
    public static readonly IReadOnlyList<string> MistakeCodes = new[]
    {
        "missing_article", "article_a_an_confusion", "no_tense_marker", "irregular_past_form",
        "present_perfect_vs_past_simple", "preposition_direct_translation", "time_preposition_confusion",
        "gerund_after_preposition", "verb_plus_wrong_form", "double_modal", "modal_plus_to",
        "third_person_s_missing", "be_verb_mismatch", "adjective_order", "question_inversion_missing",
        "false_friend", "word_repetition", "double_letter_omission", "silent_letter_omission",
        "general_uzbek_interference",
    };

    /// <summary>The cached system prompt. Public so the batch backfill reuses the identical prompt.</summary>
    public static readonly string SystemPrompt =
        $$"""
        You create grammar lessons for Uzbek learners of English, teaching ONE grammar point in the
        context of a given TOPIC. Given a TOPIC, a GRAMMAR FOCUS and a CEFR level, reply with ONLY a
        JSON object, no prose, no code fences:
        {"intro":"<contextual intro>","rule":"<rule explanation>",
         "exercises":[{"type":"recognition|fill|rephrase","q":"<prompt>","options":["<a>","<b>","<c>"],"answer":<0-based index>,"why":"<short English explanation>"}],
         "tasks":[{"skill":"speaking|writing","prompt":"<task prompt>"}],
         "examples":[{"en":"<example English sentence>","uz":"<qisqa o'zbekcha ma'nosi>"}],
         "mistakes":["<mistake code from MISTAKE CODES>","<another mistake code>"]}
        Rules:
        - "intro": 2-4 short English sentences showing the GRAMMAR FOCUS used naturally while talking
          about the TOPIC, written at the given CEFR level (A1 simplest, C2 most advanced).
        - "rule": a clear English explanation of the GRAMMAR FOCUS (how to form and use it), with
          one or two example sentences about the TOPIC. Keep it short and learner-friendly.
        - "exercises": exactly 10 multiple-choice exercises that practise the GRAMMAR FOCUS in the
          TOPIC's context. Include a mix: at least three "recognition" (spot the correct form) and at
          least three "fill" or "rephrase" (active use). Each has 2-4 options, a 0-based "answer"
          index, and "why": a short English explanation of why that answer is correct.
        - "tasks": exactly two production tasks - one "speaking" and one "writing" - each a short
          English prompt asking the learner to use the GRAMMAR FOCUS while talking/writing about the TOPIC.
        - "examples": 2-3 example English sentences showing the GRAMMAR FOCUS in the TOPIC's context,
          each paired with a short Uzbek meaning in "uz" (e.g. {"en":"She is reading a book.","uz":"U kitob o'qiyapti."}).
          The "en" must be English; the "uz" must be a genuine, natural Uzbek translation of that exact
          "en" sentence (not a copy of the English, not empty).
        - "mistakes": 2-3 codes naming the common mistakes Uzbek learners make with this GRAMMAR FOCUS.
          Pick ONLY from this fixed list, and pick codes that genuinely fit the GRAMMAR FOCUS - never
          invent a new code and never write the Uzbek explanation yourself:
          MISTAKE CODES: {{string.Join(", ", MistakeCodes)}}
        - If TARGET WORDS are given, use as many as fit naturally in the intro, rule examples and
          exercises so the learner practises the topic's vocabulary again - but never force a word.
        - Everything except "mistakes" (codes) and "examples[].uz" (Uzbek translation) must be in
          English. Output valid JSON only.
        - LANGUAGE RULE (MANDATORY): All fields ("intro", "rule", questions, options, "why", tasks)
          must be in English. The ONLY languages allowed are English and Uzbek. NEVER write Chinese,
          Japanese, Korean, Russian, Spanish, French, German, Arabic or ANY other language. NEVER use
          any Cyrillic or non-Latin script. NEVER add any text outside the JSON object (no commentary,
          no explanation, no code fences). Return ONLY the JSON object.
        """;

    private readonly ILlmCompletion _llm;
    private readonly IGrammarContentProvider _content;

    public LlmGrammarContentGenerator(ILlmCompletion llm, IGrammarContentProvider content)
    {
        _llm = llm;
        _content = content;
    }

    public async Task<GeneratedGrammarContent> GenerateAsync(
        string topicTitle, string grammarFocusCode, CefrLevel level,
        IReadOnlyList<string>? targetWords = null, CancellationToken cancellationToken = default)
    {
        var prompt = BuildUserPrompt(topicTitle, grammarFocusCode, level, targetWords);
        // A failed call returns null, leaving the lesson pending rather than fabricating content
        // (docs/development-guide.md rules 8, 11).
        var text = await _llm.CompleteAsync(SystemPrompt, prompt, MaxOutputTokens, cancellationToken);
        return Parse(text, _content);
    }

    /// <summary>
    /// Builds the per-topic user prompt, optionally listing the topic's vocabulary words to reuse.
    /// Public so the batch backfill builds the identical input.
    /// </summary>
    public static string BuildUserPrompt(
        string topicTitle, string grammarFocusCode, CefrLevel level,
        IReadOnlyList<string>? targetWords = null) =>
        $"TOPIC: {topicTitle}\nGRAMMAR FOCUS: {grammarFocusCode}\nCEFR LEVEL: {level}"
        + TargetWordPrompt.Line(targetWords);

    /// <summary>
    /// Parses the model's JSON reply into generated content. Tolerant of surrounding prose/fences
    /// (extracts the outermost JSON object); returns <see cref="GeneratedGrammarContent.Empty"/> on
    /// any malformed or incomplete payload. <paramref name="contentProvider"/> resolves each
    /// "mistakes" entry - which the model returns as a CODE, never Uzbek prose - to its vetted Uzbek
    /// explanation (rule 11); a code with no match (unknown/invented by the model) is dropped rather
    /// than ever surfacing raw LLM text.
    /// </summary>
    public static GeneratedGrammarContent Parse(string? response, IGrammarContentProvider contentProvider)
    {
        if (string.IsNullOrWhiteSpace(response))
            return GeneratedGrammarContent.Empty;

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            return GeneratedGrammarContent.Empty;

        var json = response.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var intro = StringProp(root, "intro");
            var rule = StringProp(root, "rule");
            if (string.IsNullOrWhiteSpace(intro) || string.IsNullOrWhiteSpace(rule))
                return GeneratedGrammarContent.Empty;
            if (!ContentLanguageGuard.IsClean(intro) || !ContentLanguageGuard.IsClean(rule))
                return GeneratedGrammarContent.Empty;

            var exercises = new List<GeneratedGrammarExercise>();
            if (root.TryGetProperty("exercises", out var exEl) && exEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in exEl.EnumerateArray())
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

                    exercises.Add(new GeneratedGrammarExercise(
                        ExerciseType(StringProp(entry, "type")),
                        q!.Trim(), options, answer,
                        string.IsNullOrWhiteSpace(why) ? null : why!.Trim()));
                }
            }

            if (exercises.Count == 0)
                return GeneratedGrammarContent.Empty;

            var tasks = new List<GeneratedGrammarApplicationTask>();
            if (root.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in tasksEl.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        continue;

                    var prompt = StringProp(entry, "prompt");
                    if (string.IsNullOrWhiteSpace(prompt))
                        continue;
                    if (!ContentLanguageGuard.IsClean(prompt))
                        continue;

                    tasks.Add(new GeneratedGrammarApplicationTask(TargetSkill(StringProp(entry, "skill")), prompt!.Trim()));
                }
            }

            // Example sentences (English + Uzbek meaning) and common Uzbek-learner mistakes. Both are
            // optional in the payload - a lesson is still valid without them, so we never fail the
            // whole parse if they are missing (rules 8, 11).
            var examples = new List<GeneratedGrammarExample>();
            if (root.TryGetProperty("examples", out var exsEl) && exsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in exsEl.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        continue;
                    var en = StringProp(entry, "en");
                    var uz = StringProp(entry, "uz");
                    if (string.IsNullOrWhiteSpace(en))
                        continue;
                    if (!ContentLanguageGuard.IsClean(en))
                        continue;
                    // The Uzbek meaning is learner-facing and dynamic (a translation of the model's
                    // own English example, so it can't be a fixed template lookup like "mistakes").
                    // The second-pass heuristic guard rejects degenerate output - empty, wrong
                    // script, an exact copy of the English, or implausibly short - dropping the
                    // whole example rather than shipping a broken translation (rule 11).
                    if (!ContentLanguageGuard.IsPlausibleTranslation(en, uz))
                        continue;
                    examples.Add(new GeneratedGrammarExample(en!.Trim(), uz!.Trim()));
                }
            }

            // The model returns mistake CODES, never Uzbek prose (rule 11). A code that doesn't
            // resolve to a vetted template - unknown, misspelled or invented by the model - is
            // silently dropped rather than ever falling back to the model's raw string.
            var mistakes = new List<string>();
            if (root.TryGetProperty("mistakes", out var misEl) && misEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in misEl.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.String)
                        continue;
                    var code = entry.GetString();
                    var explanation = contentProvider.GetMistakeExplanation(code);
                    if (!string.IsNullOrWhiteSpace(explanation))
                        mistakes.Add(explanation);
                }
            }

            return new GeneratedGrammarContent(intro!.Trim(), rule!.Trim(), examples, mistakes, exercises, tasks);
        }
        catch (JsonException)
        {
            return GeneratedGrammarContent.Empty;
        }
    }

    private static GrammarExerciseType ExerciseType(string? type) => (type?.Trim().ToLowerInvariant()) switch
    {
        "fill" or "fill-in-blank" or "fillinblank" or "gap" => GrammarExerciseType.FillInBlank,
        "rephrase" or "rewrite" or "transform" => GrammarExerciseType.Rephrase,
        _ => GrammarExerciseType.Recognition,
    };

    private static SkillType TargetSkill(string? skill) =>
        string.Equals(skill?.Trim(), "speaking", StringComparison.OrdinalIgnoreCase)
            ? SkillType.Speaking
            : SkillType.Writing;

    private static string? StringProp(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
