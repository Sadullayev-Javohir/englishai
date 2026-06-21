using System.Text.Json;
using Application.Books.Models;
using Application.Books.Ports;
using Domain.Assessment;
using Domain.Books;
using Infrastructure.Llm;

namespace Infrastructure.Books;

/// <summary>
/// LLM-backed book-section generator (provider-agnostic via <see cref="ILlmCompletion"/> - Gemini by
/// default, rule 10). Given a book, a section
/// title and a CEFR level it authors a cohesive English passage that continues the story and
/// exactly <see cref="BookSection.QuestionsPerSection"/> comprehension questions with English
/// explanations - the sanctioned dynamic path of docs/development-guide.md rule 11 (target-language teaching
/// content, structured JSON). Cost is controlled (rule 10) by a budget model, a cached system
/// prompt and a capped output. Any failure yields <see cref="GeneratedBookSection.Empty"/> so the
/// section stays honestly "pending" (rules 8, 11).
/// </summary>
public sealed class LlmBookContentGenerator : IBookContentGenerator
{
    /// <summary>Output-token cap (docs/development-guide.md rule 10). Larger than a single reading lesson because a
    /// section carries ten questions.</summary>
    public const int MaxOutputTokens = 4096;

    /// <summary>The cached system prompt.</summary>
    public const string SystemPrompt =
        """
        You write one section (chapter) of a graded reader for Uzbek learners of English.
        Given the BOOK title, its SYNOPSIS, the SECTION title, the section number out of the total,
        and a CEFR level, reply with ONLY a JSON object, no prose, no code fences:
        {"body":"<section text>",
         "questions":[{"q":"<question>","options":["<a>","<b>","<c>","<d>"],"answer":<0-based index>,"why":"<short English explanation>"}]}
        Rules:
        - "body": one cohesive, engaging English passage that tells this section of the story and fits
          the book's synopsis. Write it at the given CEFR level (A1 simplest, C2 most advanced).
          A1/A2: 120-200 words; B1/B2: 200-300 words; C1/C2: 280-380 words. Keep continuity with the
          section title and the book; do not restate the synopsis verbatim.
        - "questions": EXACTLY 10 comprehension questions answerable from this section's text. Mix
          main-idea, detail, vocabulary-in-context and inference. Each has 3-4 options, a 0-based
          "answer" index, and "why": a short English explanation of why that answer is correct.
        - Everything is in English. Output valid JSON only with exactly 10 questions.
        """;

    private readonly ILlmCompletion _llm;

    public LlmBookContentGenerator(ILlmCompletion llm) => _llm = llm;

    public async Task<GeneratedBookSection> GenerateAsync(
        string bookTitle,
        string synopsis,
        string sectionTitle,
        int sectionNumber,
        int totalSections,
        CefrLevel level,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildUserPrompt(bookTitle, synopsis, sectionTitle, sectionNumber, totalSections, level);
        // A failed call returns null, leaving the section pending rather than fabricating content
        // (docs/development-guide.md rules 8, 11).
        var text = await _llm.CompleteAsync(SystemPrompt, prompt, MaxOutputTokens, cancellationToken);
        return Parse(text);
    }

    /// <summary>Builds the per-section user prompt. Public so a batch backfill builds the same input.</summary>
    public static string BuildUserPrompt(
        string bookTitle, string synopsis, string sectionTitle, int sectionNumber, int totalSections, CefrLevel level) =>
        $"BOOK: {bookTitle}\nSYNOPSIS: {synopsis}\nSECTION: {sectionTitle}\n" +
        $"SECTION NUMBER: {sectionNumber} of {totalSections}\nCEFR LEVEL: {level}";

    /// <summary>
    /// Parses the model's JSON reply. Tolerant of surrounding prose/fences (extracts the outermost
    /// JSON object); returns <see cref="GeneratedBookSection.Empty"/> on any malformed or incomplete
    /// payload so the section stays pending.
    /// </summary>
    public static GeneratedBookSection Parse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return GeneratedBookSection.Empty;

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            return GeneratedBookSection.Empty;

        var json = response.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var body = StringProp(root, "body");
            if (string.IsNullOrWhiteSpace(body))
                return GeneratedBookSection.Empty;

            var questions = new List<GeneratedBookQuestion>();
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

                    questions.Add(new GeneratedBookQuestion(
                        q!.Trim(), options, answer, string.IsNullOrWhiteSpace(why) ? null : why!.Trim()));
                }
            }

            return questions.Count == 0
                ? GeneratedBookSection.Empty
                : new GeneratedBookSection(body!.Trim(), questions);
        }
        catch (JsonException)
        {
            return GeneratedBookSection.Empty;
        }
    }

    private static string? StringProp(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
