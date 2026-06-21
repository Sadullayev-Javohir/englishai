using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Writing.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Writing;
using Infrastructure.Llm;

namespace Infrastructure.Writing;

/// <summary>
/// LLM-backed writing assessor (PROJECT-SPEC G.3), routed through the internal Hermes Agent Gateway. It asks
/// the model to score the four dimensions and return located issues as strict, structured JSON - never
/// free-form Uzbek (docs/development-guide.md rule 11); the Uzbek explanations are resolved from vetted templates by
/// their codes. Uses a budget model with a hard output-token cap. Selected over the Local assessor only
/// when the gateway key is configured.
/// </summary>
public sealed class HermesWritingAssessor : ITopicWritingAssessor
{
    private readonly ILlmCompletion _completion;

    public HermesWritingAssessor(ILlmCompletion completion)
    {
        _completion = completion;
    }

    public async Task<WritingAssessment> AssessAsync(
        WritingTask task,
        string text,
        CefrLevel assessmentLevel,
        CancellationToken cancellationToken)
        => await AssessOnceAsync(task, text, assessmentLevel, cancellationToken);

    private async Task<WritingAssessment> AssessOnceAsync(
        WritingTask task,
        string text,
        CefrLevel assessmentLevel,
        CancellationToken cancellationToken)
    {
        var userPrompt =
            $"Assessment target: CEFR {assessmentLevel}.\n" +
            $"Task source level: CEFR {task.Level}.\n" +
            $"Task: {task.Prompt}\n\nLearner's submission:\n{text}";
        var json = await _completion.CompleteAsync(SystemPrompt, userPrompt, 800, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Writing assessment gateway returned no usable reply.");

        json = json.Trim();

        var parsed = Parse(json);
        return BuildAssessment(task.Id, parsed);
    }


    private const string SystemPrompt =
        """
        You are a CEFR writing examiner. Assess the learner's English text on four
        dimensions, each scored 1-5: task achievement, coherence, lexical resource,
        grammatical accuracy.

        Grade RELATIVE TO the explicit "Assessment target" CEFR level in the user message,
        never against native/C2 mastery. A clear response that fully meets that target level's
        can-do expectations must receive 5/5 even when it uses simple vocabulary and grammar.
        A1 expects short simple sentences about familiar personal topics; it does not require
        linking devices, complex clauses, advanced vocabulary, or native-like variety. A2 may
        use simple connected sentences and common everyday vocabulary. Only demand progressively
        broader organization, vocabulary and grammar from B1 upward. Report issues only when they
        are genuine errors or target-level requirements; do not suggest C1/C2 improvements to an
        A1/A2 learner.

        Return ONLY a JSON object, no prose, with this exact shape:
        {
          "dimensions": { "task": 1-5, "coherence": 1-5, "lexical": 1-5, "grammar": 1-5 },
          "issues": [
            { "dimension": "task|coherence|lexical|grammar",
              "code": "<snake_case issue code>",
              "start": <char offset>, "end": <char offset>,
              "category": "Articles|VerbTense|Prepositions|GerundInfinitive|Modals|SubjectVerbAgreement|WordOrder|Vocabulary|Spelling|null" }
          ]
        }

        Use only these issue codes: subject_verb_agreement, missing_article,
        wrong_preposition, verb_tense, gerund_infinitive, word_choice, spelling,
        off_topic, weak_coherence, too_short. Set "category" only for grammar issues
        (otherwise null). Do not write any Uzbek or explanatory text.
        """;

    internal static WritingAssessment BuildAssessment(Guid taskId, AssessmentJson parsed)
    {
        var d = parsed.Dimensions
            ?? throw new InvalidOperationException("Writing assessment response had no dimension scores.");

        var scores = new[]
        {
            new DimensionScore(WritingDimension.TaskAchievement, d.Task),
            new DimensionScore(WritingDimension.Coherence, d.Coherence),
            new DimensionScore(WritingDimension.LexicalResource, d.Lexical),
            new DimensionScore(WritingDimension.GrammaticalAccuracy, d.Grammar),
        };

        var issues = (parsed.Issues ?? new List<IssueJson>())
            .Where(i => !string.IsNullOrWhiteSpace(i.Code))
            .Select(i => new WritingIssue(
                MapDimension(i.Dimension),
                $"writing.issue.{i.Code}",
                Math.Max(0, i.Start),
                Math.Max(i.Start, i.End),
                MapCategory(i.Category)))
            .ToList();

        return WritingAssessment.Create(taskId, scores, issues);
    }

    internal static AssessmentJson Parse(string response)
    {
        // Budget models occasionally wrap the JSON in markdown code fences or surrounding
        // prose despite the instruction not to (docs/development-guide.md rule 11 keeps output structured,
        // but we stay tolerant). Extract the outermost JSON object before deserializing -
        // same approach as LlmVocabularyPassageGenerator.
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Writing assessment response contained no JSON object.");

        var json = response.Substring(start, end - start + 1);

        try
        {
            return JsonSerializer.Deserialize<AssessmentJson>(json, JsonOptions)
                   ?? throw new InvalidOperationException("Empty writing assessment response.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Writing assessment response was not valid JSON.", ex);
        }
    }

    private static WritingDimension MapDimension(string? dimension) => dimension?.ToLowerInvariant() switch
    {
        "task" => WritingDimension.TaskAchievement,
        "coherence" => WritingDimension.Coherence,
        "lexical" => WritingDimension.LexicalResource,
        _ => WritingDimension.GrammaticalAccuracy,
    };

    private static ErrorCategory? MapCategory(string? category) =>
        Enum.TryParse<ErrorCategory>(category, ignoreCase: true, out var parsed) ? parsed : null;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal sealed class AssessmentJson
    {
        [JsonPropertyName("dimensions")]
        public DimensionsJson? Dimensions { get; set; }

        [JsonPropertyName("issues")]
        public List<IssueJson>? Issues { get; set; }
    }

    internal sealed class DimensionsJson
    {
        [JsonPropertyName("task")] public int Task { get; set; }
        [JsonPropertyName("coherence")] public int Coherence { get; set; }
        [JsonPropertyName("lexical")] public int Lexical { get; set; }
        [JsonPropertyName("grammar")] public int Grammar { get; set; }
    }

    internal sealed class IssueJson
    {
        [JsonPropertyName("dimension")] public string? Dimension { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("start")] public int Start { get; set; }
        [JsonPropertyName("end")] public int End { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
    }
}
