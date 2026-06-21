using System.Text.RegularExpressions;
using Application.Assessment.Ports;
using Application.Speaking.Ports;
using Domain.Assessment;
using Infrastructure.Speaking;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Assessment;

/// <summary>
/// Scores a placement speaking task from speech recognition, delivery quality and
/// language evidence in the transcript. A short, simple answer can receive credit for
/// being understandable, but cannot be promoted to an advanced CEFR band solely because
/// it was pronounced clearly.
/// </summary>
public sealed partial class PlacementSpeakingAssessor : IPlacementSpeakingAssessor
{
    private static readonly string[] AdvancedConnectors =
    {
        "although", "however", "therefore", "moreover", "whereas", "nevertheless",
        "consequently", "despite", "in addition", "on the other hand", "for instance",
    };

    private static readonly IReadOnlyDictionary<string, string[]> TopicTerms =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["family"] = ["family", "mother", "father", "parent", "sister", "brother", "wife", "husband", "child", "children", "son", "daughter", "grandmother", "grandfather"],
            ["daily routine"] = ["routine", "morning", "afternoon", "evening", "wake", "breakfast", "lunch", "dinner", "work", "study", "sleep", "usually", "every day"],
            ["hobby"] = ["hobby", "activity", "enjoy", "play", "read", "watch", "listen", "sport", "music", "book", "game", "free time"],
            ["decision"] = ["decision", "decide", "choice", "choose", "changed", "life"],
            ["social media"] = ["social media", "internet", "online", "facebook", "instagram", "telegram", "tiktok", "harm", "good", "opinion"],
            ["space exploration"] = ["space", "exploration", "government", "fund", "money", "earth", "planet", "moon", "mars", "problem", "science"],
        };

    [GeneratedRegex(@"[a-z]+(?:'[a-z]+)?", RegexOptions.IgnoreCase)]
    private static partial Regex WordToken();

    [GeneratedRegex(@"\b(because|if|when|while|although|that|which|who|where|unless|since)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ClauseMarker();

    private readonly ISpeechToTextService _stt;
    private readonly IPronunciationAssessor _pronunciation;
    private readonly AzureSpeechOptions _azure;
    private readonly ILogger<PlacementSpeakingAssessor> _logger;

    public PlacementSpeakingAssessor(
        ISpeechToTextService stt,
        IPronunciationAssessor pronunciation,
        AzureSpeechOptions azure,
        ILogger<PlacementSpeakingAssessor> logger)
    {
        _stt = stt;
        _pronunciation = pronunciation;
        _azure = azure;
        _logger = logger;
    }

    public async Task<PlacementSpeakingScore> AssessAsync(
        byte[] audioContent, PlacementSpeakingTask task, CancellationToken cancellationToken = default)
    {
        if (!_azure.IsConfigured)
            return new PlacementSpeakingScore(0, PlacementSpeakingOutcome.ServiceUnavailable);

        try
        {
            var transcription = await _stt.TranscribeAsync(audioContent, cancellationToken);
            if (!transcription.IsAccepted)
                return new PlacementSpeakingScore(0, MapRejection(transcription.Rejection));
            var transcript = transcription.Text;

            if (!IsOnTopic(transcript, task))
                return new PlacementSpeakingScore(0, PlacementSpeakingOutcome.OffTopic);

            var pronunciation = await _pronunciation.AssessAsync(audioContent, transcript, cancellationToken);
            var score = CalculateScore(transcript, task, pronunciation);
            return new PlacementSpeakingScore(score, PlacementSpeakingOutcome.Scored);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Placement speaking assessment failed; returning a retryable unavailable outcome.");
            return new PlacementSpeakingScore(0, PlacementSpeakingOutcome.ServiceUnavailable);
        }
    }

    internal static bool IsOnTopic(string transcript, PlacementSpeakingTask task)
    {
        var normalizedPrompt = task.Prompt.ToLowerInvariant();
        var topic = TopicTerms.Keys.FirstOrDefault(normalizedPrompt.Contains);
        if (topic is null)
            return true;

        return TopicTerms[topic].Any(term =>
            transcript.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static PlacementSpeakingOutcome MapRejection(SpeechTranscriptionRejection rejection) => rejection switch
    {
        SpeechTranscriptionRejection.InvalidAudio => PlacementSpeakingOutcome.InvalidAudio,
        SpeechTranscriptionRejection.NoSpeech => PlacementSpeakingOutcome.NoSpeech,
        SpeechTranscriptionRejection.LowConfidence => PlacementSpeakingOutcome.LowConfidence,
        _ => PlacementSpeakingOutcome.ServiceUnavailable,
    };

    internal static int CalculateScore(
        string transcript, PlacementSpeakingTask task, Domain.Speaking.PronunciationResult pronunciation)
    {
        var words = WordToken().Matches(transcript).Select(match => match.Value.ToLowerInvariant()).ToList();
        if (words.Count == 0)
            return 0;

        var completeness = Math.Clamp((double)words.Count / task.MinWords, 0, 1);
        var uniqueRatio = (double)words.Distinct(StringComparer.OrdinalIgnoreCase).Count() / words.Count;
        var lexicalScore = Math.Clamp((uniqueRatio - 0.35) / 0.45, 0, 1) * 100;
        var clauseCount = ClauseMarker().Matches(transcript).Count;
        var connectorCount = AdvancedConnectors.Count(connector =>
            transcript.Contains(connector, StringComparison.OrdinalIgnoreCase));
        var languageComplexity = Math.Clamp(
            20 + Math.Min(words.Count, 40) * 0.75 + clauseCount * 8 + connectorCount * 12,
            0,
            100);
        var deliveryScore = Math.Clamp(
            0.35 * pronunciation.AccuracyScore +
            0.50 * pronunciation.FluencyScore +
            0.15 * pronunciation.CompletenessScore,
            0,
            100);

        var rawScore =
            0.30 * deliveryScore +
            0.35 * completeness * 100 +
            0.20 * languageComplexity +
            0.15 * lexicalScore;

        var evidenceCeiling = EvidenceCeiling(words.Count, task.MinWords, clauseCount, connectorCount);
        var taskCeiling = PlacementScoring.CapProductiveScore(100, task.Difficulty);
        return Math.Clamp((int)Math.Round(rawScore), 0, Math.Min(evidenceCeiling, taskCeiling));
    }

    private static int EvidenceCeiling(int wordCount, int minimumWords, int clauseCount, int connectorCount)
    {
        if (wordCount < Math.Max(4, minimumWords / 2))
            return 43;
        if (wordCount < minimumWords || clauseCount == 0)
            return 59;
        if (wordCount < 35 || clauseCount < 2)
            return 75;
        if (wordCount < 50 || connectorCount == 0)
            return 89;
        return 100;
    }

}
