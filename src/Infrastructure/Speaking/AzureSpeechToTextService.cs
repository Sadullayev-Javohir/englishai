using Application.Speaking.Ports;
using Application.Ai;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Infrastructure.Speaking;

/// <summary>
/// Azure Speech-to-Text adapter. Expects 16 kHz, 16-bit, mono PCM audio (the Azure
/// push-stream default). Returns the full recognized transcript, or an empty string when
/// nothing is recognized.
/// </summary>
public sealed class AzureSpeechToTextService : IContextualSpeechToTextService, IStreamingSpeechToTextService
{
    private readonly AzureSpeechOptions _options;
    private readonly ILogger<AzureSpeechToTextService> _logger;
    private readonly IVariableCostMeter _costs;

    public AzureSpeechToTextService(
        AzureSpeechOptions options,
        ILogger<AzureSpeechToTextService> logger,
        IVariableCostMeter costs)
    {
        _options = options;
        _logger = logger;
        _costs = costs;
    }

    public Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent,
        CancellationToken cancellationToken = default) =>
        TranscribeAsync(audioContent, EmptyContext, cancellationToken);

    public async Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent,
        SpeechRecognitionContext context,
        CancellationToken cancellationToken = default)
    {
        if (!WavAudio.TryExtractRequiredPcm(audioContent, out var pcm))
        {
            _logger.LogWarning("Speaking transcription rejected because the audio is not 16 kHz mono PCM WAV.");
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.InvalidAudio);
        }
        if (!WavAudio.HasAudibleSignal(pcm))
        {
            _logger.LogInformation("Speaking transcription rejected because the clip has no audible signal.");
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech);
        }

        var admission = AiAdmissionContext.Current;
        _costs.EnsureAllowed(admission.Tier, AiFeature.SpeakingEvaluation, admission.CallerKey);
        var audioHours = WavAudio.DurationSeconds(pcm) / 3600d;

        try
        {
            return await TranscribeCoreAsync(pcm, context, cancellationToken);
        }
        finally
        {
            _costs.Record(
                VariableCostCategory.SpeechToText,
                audioHours,
                "audio_hour",
                audioHours * Math.Max(0, _options.SpeechToTextCostPerAudioHourUsd),
                admission.CallerKey,
                AiFeature.SpeakingEvaluation,
                admission.RequestPath);
        }
    }

    public IStreamingSpeechToTextSession CreateSession(
        SpeechRecognitionContext context,
        Func<string, Task>? onPartialTranscript = null) =>
        new AzureStreamingSpeechToTextSession(_options, _logger, _costs, context, onPartialTranscript);

    private async Task<SpeechTranscription> TranscribeCoreAsync(
        byte[] pcm,
        SpeechRecognitionContext context,
        CancellationToken cancellationToken)
    {

        var speechConfig = SpeechConfig.FromSubscription(_options.Key, _options.Region);
        speechConfig.SpeechRecognitionLanguage = string.IsNullOrWhiteSpace(context.RecognitionLanguage)
            ? _options.RecognitionLanguage
            : context.RecognitionLanguage;
        speechConfig.OutputFormat = OutputFormat.Detailed;

        using var pushStream = AudioInputStream.CreatePushStream();
        pushStream.Write(pcm);
        pushStream.Close();

        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var phraseList = PhraseListGrammar.FromRecognizer(recognizer);
        var contextPhrases = ContextPhraseExtractor.Extract(context);
        foreach (var phrase in contextPhrases)
            phraseList.AddPhrase(phrase);
        if (contextPhrases.Count > 0)
            phraseList.SetWeight(Math.Max(1.25, context.IncludeNameHints ? _options.NamePhraseWeight : 1.65));
        if (context.IncludeNameHints)
        {
            foreach (var name in UzbekNamePhrases.Names)
                phraseList.AddPhrase(name);
            phraseList.SetWeight(Math.Max(1.65, _options.NamePhraseWeight));
        }

        // Continuous recognition (not RecognizeOnce, which stops at the first sentence/pause): the
        // learner may speak several sentences or a whole paragraph in one recording, so we collect
        // every recognized segment until the audio stream ends and join them into one transcript.
        var segments = new List<IReadOnlyList<SpeechTranscriptionCandidate>>();
        var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        recognizer.Recognized += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizedSpeech || string.IsNullOrWhiteSpace(e.Result.Text))
                return;

            var candidates = e.Result.Best()
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Text))
                .Select(candidate => new SpeechTranscriptionCandidate(candidate.Text.Trim(), candidate.Confidence))
                .DistinctBy(candidate => TranscriptText.Normalize(candidate.Text))
                .ToArray();
            if (candidates.Length > 0) segments.Add(candidates);
        };
        // The session ends when the pushed stream is exhausted; cancellation also covers an error.
        recognizer.SessionStopped += (_, _) => stopped.TrySetResult(true);
        recognizer.Canceled += (_, eventArgs) =>
        {
            _logger.LogWarning(
                "Azure speech recognition was cancelled with reason {Reason} and code {ErrorCode}.",
                eventArgs.Reason,
                eventArgs.ErrorCode);
            stopped.TrySetResult(true);
        };

        using var registration = cancellationToken.Register(() => stopped.TrySetResult(true));

        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
        await stopped.Task.ConfigureAwait(false);
        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

        if (segments.Count == 0)
        {
            _logger.LogInformation("Speaking transcription rejected because Azure recognized no speech.");
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech);
        }

        var composed = TranscriptAlternativeComposer.Compose(
            segments,
            Math.Max(2, _options.MaximumTranscriptAlternatives));
        var selection = ContextualTranscriptSelector.Select(
            composed,
            context,
            _options.ConfirmationConfidenceThreshold,
            _options.ConfirmationScoreGap,
            _options.MaximumTranscriptAlternatives);

        if (selection is null)
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech);
        if (selection.Confidence < _options.MinimumRecognitionConfidence)
        {
            _logger.LogInformation(
                "Speaking transcription rejected because confidence {Confidence:F3} is below {Minimum:F3}.",
                selection.Confidence,
                _options.MinimumRecognitionConfidence);
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.LowConfidence);
        }

        var selectedText = ContextualTranscriptCorrections.Apply(selection.Text, context);

        return SpeechTranscription.Accepted(
            selectedText,
            selection.Confidence,
            selection.Alternatives,
            selection.RequiresConfirmation,
            selection.ScoreGap);
    }

    private static readonly SpeechRecognitionContext EmptyContext =
        new(null, null, Array.Empty<string>(), Array.Empty<string>(), IncludeNameHints: false);
}

internal static class ContextualTranscriptCorrections
{
    public static string Apply(string text, SpeechRecognitionContext context)
    {
        if (!IsEnglishLevelContext(context)) return text;

        var corrected = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(?i)\b(my\s+english\s+level\s+is|i\s+am|i'm)\s+(?:iran|a\s*one|ay\s*one)\b",
            match => $"{match.Groups[1].Value} A1");
        corrected = System.Text.RegularExpressions.Regex.Replace(
            corrected,
            @"(?i)\b(my\s+english\s+level\s+is|i\s+am|i'm)\s+(a\s*two|b\s*one|b\s*two|c\s*one|c\s*two)\b",
            match => $"{match.Groups[1].Value} {NormalizeLevel(match.Groups[2].Value)}");
        return corrected;
    }

    private static bool IsEnglishLevelContext(SpeechRecognitionContext context) =>
        new[] { context.LastTutorPrompt, context.Topic }
            .Concat(context.ContextPhrases)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Any(value => value!.Contains("english level", StringComparison.OrdinalIgnoreCase)
                || value.Contains("cefr", StringComparison.OrdinalIgnoreCase));

    private static string NormalizeLevel(string level) =>
        string.Concat(level.Where(char.IsLetterOrDigit)).ToUpperInvariant() switch
        {
            "ATWO" => "A2",
            "BONE" => "B1",
            "BTWO" => "B2",
            "CONE" => "C1",
            "CTWO" => "C2",
            var normalized => normalized,
        };
}

internal sealed record ContextualTranscriptSelection(
    string Text,
    double Confidence,
    IReadOnlyList<SpeechTranscriptionCandidate> Alternatives,
    bool RequiresConfirmation,
    double ScoreGap);

internal static class ContextualTranscriptSelector
{
    public static ContextualTranscriptSelection? Select(
        IReadOnlyList<SpeechTranscriptionCandidate> candidates,
        SpeechRecognitionContext context,
        double confirmationConfidenceThreshold,
        double confirmationScoreGap,
        int maximumAlternatives)
    {
        var ranked = candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Text))
            .Select(candidate => new RankedCandidate(
                candidate,
                Score(candidate, context)))
            .GroupBy(candidate => TranscriptText.Normalize(candidate.Candidate.Text), StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(candidate => candidate.Score).First())
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Candidate.Confidence)
            .ToArray();
        if (ranked.Length == 0) return null;

        var best = ranked[0];
        var scoreGap = ranked.Length > 1 ? best.Score - ranked[1].Score : 1d;
        var alternatives = ranked
            .Take(Math.Clamp(maximumAlternatives, 1, 5))
            .Select(candidate => candidate.Candidate)
            .ToArray();
        var requiresConfirmation = best.Candidate.Confidence < confirmationConfidenceThreshold
            || (ranked.Length > 1 && scoreGap < confirmationScoreGap);

        return new ContextualTranscriptSelection(
            best.Candidate.Text,
            best.Candidate.Confidence,
            alternatives,
            requiresConfirmation,
            scoreGap);
    }

    private static double Score(
        SpeechTranscriptionCandidate candidate,
        SpeechRecognitionContext context)
    {
        var normalizedCandidate = TranscriptText.Words(candidate.Text);
        if (normalizedCandidate.Count == 0) return candidate.Confidence;

        var contextTokens = context.ContextPhrases
            .SelectMany(TranscriptText.Words)
            .ToHashSet(StringComparer.Ordinal);
        var overlap = normalizedCandidate.Count(token => contextTokens.Contains(token));
        var overlapRatio = overlap / (double)normalizedCandidate.Count;
        var phraseBonus = ContextPhraseExtractor.Extract(context).Any(phrase =>
            TranscriptText.Normalize(candidate.Text).Contains(TranscriptText.Normalize(phrase), StringComparison.Ordinal)
            || TranscriptText.Normalize(phrase).Contains(TranscriptText.Normalize(candidate.Text), StringComparison.Ordinal))
            ? 0.08
            : 0;

        return candidate.Confidence + overlapRatio * 0.28 + phraseBonus;
    }

    private sealed record RankedCandidate(SpeechTranscriptionCandidate Candidate, double Score);
}

internal static class TranscriptAlternativeComposer
{
    public static IReadOnlyList<SpeechTranscriptionCandidate> Compose(
        IReadOnlyList<IReadOnlyList<SpeechTranscriptionCandidate>> segments,
        int maximumAlternatives)
    {
        IReadOnlyList<SpeechTranscriptionCandidate> composed =
            new[] { new SpeechTranscriptionCandidate(string.Empty, 1d) };

        foreach (var segment in segments)
        {
            composed = composed
                .SelectMany(prefix => segment.Select(candidate => new SpeechTranscriptionCandidate(
                    string.Join(' ', new[] { prefix.Text, candidate.Text }.Where(text => !string.IsNullOrWhiteSpace(text))),
                    WeightedConfidence(prefix, candidate))))
                .GroupBy(candidate => TranscriptText.Normalize(candidate.Text), StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(candidate => candidate.Confidence).First())
                .OrderByDescending(candidate => candidate.Confidence)
                .Take(Math.Max(2, maximumAlternatives))
                .ToArray();
        }

        return composed;
    }

    private static double WeightedConfidence(
        SpeechTranscriptionCandidate prefix,
        SpeechTranscriptionCandidate candidate)
    {
        var prefixLength = Math.Max(0, prefix.Text.Length);
        var candidateLength = Math.Max(1, candidate.Text.Length);
        var totalLength = prefixLength + candidateLength;
        return totalLength == 0
            ? candidate.Confidence
            : (prefix.Confidence * prefixLength + candidate.Confidence * candidateLength) / totalLength;
    }
}

internal static class ContextPhraseExtractor
{
    public static IReadOnlyList<string> Extract(SpeechRecognitionContext context) =>
        context.ContextPhrases
            .SelectMany(phrase => new[] { phrase }.Concat(TranscriptText.Phrases(phrase, 4)))
            .Concat(context.FocusWords)
            .Select(phrase => phrase.Trim())
            .Where(phrase => phrase.Length is >= 2 and <= 80)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(80)
            .ToArray();
}

internal static class TranscriptText
{
    public static string Normalize(string value) =>
        string.Join(' ', Words(value));

    public static IReadOnlyList<string> Words(string value) =>
        value.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', '.', ',', '!', '?', ':', ';', '"', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => word.Trim('\'', '-'))
            .Where(word => word.Length > 0)
            .ToArray();

    public static IEnumerable<string> Phrases(string value, int maximumWords)
    {
        var words = Words(value);
        for (var size = Math.Min(maximumWords, words.Count); size >= 2; size--)
        {
            for (var index = 0; index <= words.Count - size; index++)
                yield return string.Join(' ', words.Skip(index).Take(size));
        }
    }
}
