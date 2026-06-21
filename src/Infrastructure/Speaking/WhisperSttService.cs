using System.Net.Http.Headers;
using System.Text.Json;
using Application.Ai;
using Application.Speaking.Ports;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

/// <summary>
/// Speech-to-text via the self-hosted sidecar (services/speech-stt), used for free accounts.
///
/// Cost is recorded at zero: the request runs on our own VPS, so it consumes no metered vendor
/// capacity. It is still recorded, so the founder dashboard shows how much traffic the free tier
/// moved off Azure rather than that traffic simply vanishing from the report.
/// </summary>
public sealed class WhisperSttService : IContextualSpeechToTextService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SpeechRecognitionContext EmptyContext =
        new(null, null, Array.Empty<string>(), Array.Empty<string>(), false);

    private readonly HttpClient _http;
    private readonly WhisperSttOptions _options;
    private readonly ILogger<WhisperSttService> _logger;
    private readonly IVariableCostMeter _costs;

    public WhisperSttService(
        HttpClient http,
        WhisperSttOptions options,
        ILogger<WhisperSttService> logger,
        IVariableCostMeter costs)
    {
        _http = http;
        _options = options;
        _logger = logger;
        _costs = costs;
    }

    public Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent, CancellationToken cancellationToken = default) =>
        TranscribeAsync(audioContent, EmptyContext, cancellationToken);

    public async Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent,
        SpeechRecognitionContext context,
        CancellationToken cancellationToken = default)
    {
        // The same pre-flight rejections the Azure adapter applies, so a learner gets identical
        // feedback for a broken microphone regardless of which tier transcribed them.
        if (!WavAudio.TryExtractRequiredPcm(audioContent, out var pcm))
        {
            _logger.LogWarning("Whisper transcription rejected because the audio is not 16 kHz mono PCM WAV.");
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.InvalidAudio);
        }
        if (!WavAudio.HasAudibleSignal(pcm))
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech);

        var admission = AiAdmissionContext.Current;
        var audioHours = WavAudio.DurationSeconds(pcm) / 3600d;

        try
        {
            var reply = await PostAsync(audioContent, context, cancellationToken);
            if (reply is null)
                return SpeechTranscription.Rejected(SpeechTranscriptionRejection.ServiceFailure);

            if (string.IsNullOrWhiteSpace(reply.Text))
                return SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech);

            var confidence = reply.Confidence ?? _options.MinimumConfidence;
            if (confidence < _options.MinimumConfidence)
                return SpeechTranscription.Rejected(SpeechTranscriptionRejection.LowConfidence);

            // No N-best alternatives: the sidecar returns a single reading, so the transcript
            // confirmation flow (which needs competing candidates) never triggers here.
            return SpeechTranscription.Accepted(reply.Text.Trim(), confidence);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Never poison the caller: a sidecar problem reads as a service failure so the tiered
            // router can fall back to Azure rather than losing the learner's turn.
            _logger.LogWarning(exception, "Whisper speech-to-text sidecar failed.");
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.ServiceFailure);
        }
        finally
        {
            _costs.Record(
                VariableCostCategory.SpeechToTextSelfHosted,
                audioHours,
                "audio_hour",
                estimatedCostUsd: 0,
                admission.CallerKey,
                AiFeature.SpeakingEvaluation,
                admission.RequestPath);
        }
    }

    private async Task<Response?> PostAsync(
        byte[] audioContent,
        SpeechRecognitionContext context,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        using var audio = new ByteArrayContent(audioContent);
        audio.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
        form.Add(audio, "file", "utterance.wav");
        form.Add(new StringContent(context.RecognitionLanguage ?? "en"), "language");

        // The same phrase list the Azure adapter feeds to its phrase-list grammar - topic vocabulary
        // and the learner's name, which are exactly the words a general model gets wrong.
        var prompt = string.Join(", ", ContextPhraseExtractor.Extract(context));
        if (!string.IsNullOrWhiteSpace(prompt))
            form.Add(new StringContent(prompt), "prompt");

        using var response = await _http.PostAsync("/v1/transcribe", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Whisper speech-to-text sidecar returned {StatusCode}.", (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<Response>(stream, JsonOptions, cancellationToken);
    }

    private sealed record Response(string? Text, double? Confidence, double DurationSeconds, string? ModelVersion);
}
