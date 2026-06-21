using System.Text.Json;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using Infrastructure.Llm;

namespace Infrastructure.Vocabulary;

/// <summary>
/// LLM-backed grader for the <see cref="MiniTestType.WrittenUsage"/> SRS mini-test
/// (PROJECT-SPEC B.1), provider-agnostic via <see cref="ILlmCompletion"/> (a cheap content model
/// is enough for this - docs/development-guide.md rule 10 - unlike the premium Claude essay assessor). Asks the
/// model for a strict pass/fail plus one of a fixed set of reason codes; never lets it write
/// free Uzbek prose (rule 11). A malformed/empty reply throws so
/// <see cref="ResilientWordUsageAssessor"/> can fall back to the deterministic offline assessor.
/// </summary>
public sealed class LlmWordUsageAssessor : IWordUsageAssessor
{
    /// <summary>Output-token cap (docs/development-guide.md rule 10) - the reply is one tiny JSON object.</summary>
    public const int MaxOutputTokens = 200;

    public const string SystemPrompt =
        """
        You check whether a learner's English sentence correctly uses a given target word. Reply
        with ONLY a JSON object, no prose, no code fences:
        {"correct":true|false,"reason":"correct|word_not_used|wrong_meaning|too_short|not_english"}
        Rules:
        - "correct": true only if the sentence is genuine English, is a real sentence (not just the
          bare word or a random fragment), and uses the TARGET WORD (or a natural inflection of it,
          e.g. plural/verb tense) with the meaning given by UZBEK MEANING.
        - "reason": "correct" when correct is true. When correct is false, pick the single
          best-fitting reason: "word_not_used" (the target word/inflection is absent from the
          sentence), "wrong_meaning" (the word appears but is used with the wrong sense/part of
          speech), "too_short" (trivial - not a real sentence), "not_english" (the sentence is not
          English).
        - Output valid JSON only. No commentary, no explanation, no code fences.
        """;

    private readonly ILlmCompletion _llm;

    public LlmWordUsageAssessor(ILlmCompletion llm) => _llm = llm;

    public async Task<WordUsageAssessment> AssessAsync(
        string word, string translation, string submittedSentence, CancellationToken cancellationToken)
    {
        var prompt =
            $"TARGET WORD: {word}\nUZBEK MEANING: {translation}\nLEARNER SENTENCE: {submittedSentence}";
        var text = await _llm.CompleteAsync(SystemPrompt, prompt, MaxOutputTokens, cancellationToken);
        return Parse(text);
    }

    /// <summary>
    /// Parses the model's JSON reply. Tolerant of surrounding prose/fences (extracts the outermost
    /// JSON object, same approach as the other content generators); throws on any malformed or
    /// missing payload rather than guessing, so the resilient wrapper falls back honestly.
    /// </summary>
    internal static WordUsageAssessment Parse(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new InvalidOperationException("Word-usage assessment returned no response.");

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Word-usage assessment response contained no JSON object.");

        var json = response.Substring(start, end - start + 1);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("correct", out var correctEl) ||
                (correctEl.ValueKind != JsonValueKind.True && correctEl.ValueKind != JsonValueKind.False))
                throw new InvalidOperationException("Word-usage assessment response had no boolean 'correct'.");

            var correct = correctEl.GetBoolean();
            if (correct)
                return WordUsageAssessment.Pass();

            var reason = root.TryGetProperty("reason", out var reasonEl) &&
                         reasonEl.ValueKind == JsonValueKind.String
                ? reasonEl.GetString()
                : null;

            return WordUsageAssessment.Fail(MapReason(reason));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Word-usage assessment response was not valid JSON.", ex);
        }
    }

    private static WordUsageReasonCode MapReason(string? reason) => reason?.Trim().ToLowerInvariant() switch
    {
        "wrong_meaning" => WordUsageReasonCode.WrongMeaning,
        "too_short" => WordUsageReasonCode.TooShort,
        "not_english" => WordUsageReasonCode.NotEnglish,
        _ => WordUsageReasonCode.WordNotUsed,
    };
}
