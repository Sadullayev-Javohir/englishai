using Application.Speaking.Ports;
using Application.Ai;
using Infrastructure.Ai;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using DomainSpeaking = Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// Azure Pronunciation Assessment adapter. Scores the learner's audio against the
/// recognized reference text at phoneme granularity (miscue detection on, per
/// PROJECT-SPEC E.5 - no false "correct").
/// </summary>
public sealed class AzurePronunciationAssessor : IPronunciationAssessor
{
    private readonly AzureSpeechOptions _options;
    private readonly IVariableCostMeter _costs;

    public AzurePronunciationAssessor(AzureSpeechOptions options, IVariableCostMeter costs)
    {
        _options = options;
        _costs = costs;
    }

    public async Task<DomainSpeaking.PronunciationResult> AssessAsync(
        byte[] audioContent,
        string referenceText,
        CancellationToken cancellationToken = default)
    {
        var pcm = WavAudio.ExtractPcm(audioContent);
        var admission = AiAdmissionContext.Current;
        _costs.EnsureAllowed(admission.Tier, AiFeature.SpeakingEvaluation, admission.CallerKey);
        var audioHours = WavAudio.DurationSeconds(pcm) / 3600d;

        try
        {
            return await AssessCoreAsync(pcm, referenceText, cancellationToken);
        }
        finally
        {
            _costs.Record(
                VariableCostCategory.Pronunciation,
                audioHours,
                "audio_hour",
                audioHours * Math.Max(0, _options.PronunciationCostPerAudioHourUsd),
                admission.CallerKey,
                AiFeature.SpeakingEvaluation,
                admission.RequestPath);
        }
    }

    private async Task<DomainSpeaking.PronunciationResult> AssessCoreAsync(
        byte[] pcm,
        string referenceText,
        CancellationToken cancellationToken)
    {
        var speechConfig = SpeechConfig.FromSubscription(_options.Key, _options.Region);
        speechConfig.SpeechRecognitionLanguage = _options.RecognitionLanguage;

        using var pushStream = AudioInputStream.CreatePushStream();
        pushStream.Write(pcm);
        pushStream.Close();

        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var assessmentConfig = new PronunciationAssessmentConfig(
            referenceText,
            GradingSystem.HundredMark,
            Granularity.Phoneme,
            enableMiscue: true);
        assessmentConfig.ApplyTo(recognizer);

        var result = await recognizer.RecognizeOnceAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        var assessment = PronunciationAssessmentResult.FromResult(result);

        var words = assessment.Words?.Select(MapWord).ToList()
                    ?? new List<DomainSpeaking.WordPronunciation>();

        return new DomainSpeaking.PronunciationResult(
            overallScore: assessment.PronunciationScore,
            accuracyScore: assessment.AccuracyScore,
            fluencyScore: assessment.FluencyScore,
            completenessScore: assessment.CompletenessScore,
            words: words);
    }

    private static DomainSpeaking.WordPronunciation MapWord(PronunciationAssessmentWordResult word)
    {
        var phonemes = word.Phonemes?
            .Select(p => new DomainSpeaking.PhonemePronunciation(p.Phoneme, p.AccuracyScore))
            .ToList();

        return new DomainSpeaking.WordPronunciation(
            word.Word,
            word.AccuracyScore,
            MapErrorType(word.ErrorType),
            phonemes);
    }

    private static DomainSpeaking.PronunciationErrorType MapErrorType(string? errorType) => errorType switch
    {
        "Mispronunciation" => DomainSpeaking.PronunciationErrorType.Mispronunciation,
        "Omission" => DomainSpeaking.PronunciationErrorType.Omission,
        "Insertion" => DomainSpeaking.PronunciationErrorType.Insertion,
        _ => DomainSpeaking.PronunciationErrorType.None,
    };
}
