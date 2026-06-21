using System.Net;
using System.Text;
using Application.Ai;
using Application.Speaking.Ports;
using FluentAssertions;
using Infrastructure.Ai;
using Infrastructure.Speaking;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// Routing free learners to the self-hosted sidecar is what makes the free tier affordable, and
/// falling back to the metered provider is what stops a dead sidecar from silencing them. Both
/// directions are asserted, plus the failure being loud rather than silent.
/// </summary>
public class TieredSpeechToTextServiceTests
{
    private const string SidecarTranscript = "I wake up early";

    [Fact]
    public async Task A_free_learner_is_transcribed_by_the_self_hosted_sidecar()
    {
        var (service, azureCalls) = Create(StubResponse(SidecarTranscript, confidence: 0.8));

        var result = await Transcribe(service, AiSubscriptionTier.Free);

        result.IsAccepted.Should().BeTrue();
        result.Text.Should().Be(SidecarTranscript);
        azureCalls().Should().Be(0, "a free learner must not reach the metered provider");
    }

    [Fact]
    public async Task A_paying_learner_goes_straight_to_the_metered_provider()
    {
        // Azure's accented-speech accuracy and its N-best alternatives are what the subscription
        // buys, so Pro must never be routed to the sidecar.
        var (service, azureCalls) = Create(StubResponse(SidecarTranscript, confidence: 0.9));

        await Transcribe(service, AiSubscriptionTier.Pro);

        azureCalls().Should().Be(1);
    }

    [Fact]
    public async Task A_dead_sidecar_falls_back_rather_than_losing_the_learners_turn()
    {
        // Paying for one turn beats telling a learner their speech could not be heard.
        var (service, azureCalls) = Create(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Transcribe(service, AiSubscriptionTier.Free);

        azureCalls().Should().Be(1);
    }

    [Fact]
    public async Task A_confident_rejection_is_not_retried_on_the_metered_provider()
    {
        // Silence is silence on either provider; re-sending it would pay Azure to reach the same
        // answer, on every empty clip.
        var (service, azureCalls) = Create(StubResponse(text: "", confidence: null));

        var result = await Transcribe(service, AiSubscriptionTier.Free);

        result.IsAccepted.Should().BeFalse();
        result.Rejection.Should().Be(SpeechTranscriptionRejection.NoSpeech);
        azureCalls().Should().Be(0);
    }

    [Fact]
    public async Task A_low_confidence_reading_is_rejected_on_its_own_threshold()
    {
        var (service, _) = Create(StubResponse(SidecarTranscript, confidence: 0.05));

        var result = await Transcribe(service, AiSubscriptionTier.Free);

        result.Rejection.Should().Be(SpeechTranscriptionRejection.LowConfidence);
    }

    private static Task<SpeechTranscription> Transcribe(
        TieredSpeechToTextService service, AiSubscriptionTier tier)
    {
        using var scope = AiAdmissionContext.Push(
            new AiAdmissionContext.State("learner-1", tier, AiFeature.SpeakingEvaluation));
        return service.TranscribeAsync(Wav(seconds: 1), CancellationToken.None);
    }

    private static Func<HttpRequestMessage, HttpResponseMessage> StubResponse(
        string text, double? confidence)
    {
        var confidenceJson = confidence is null
            ? "null"
            : confidence.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var json = $"{{\"text\":\"{text}\",\"confidence\":{confidenceJson},"
                   + "\"durationSeconds\":1.0,\"modelVersion\":\"test\"}";
        return _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static (TieredSpeechToTextService Service, Func<int> AzureCalls) Create(
        Func<HttpRequestMessage, HttpResponseMessage> sidecar)
    {
        var options = new WhisperSttOptions { Enabled = true, MinimumConfidence = 0.25 };
        var http = new HttpClient(new StubHandler(sidecar)) { BaseAddress = new Uri("http://speech-stt:8080") };
        var costs = new VariableCostMeter(new AiAdmissionOptions(), TimeProvider.System);
        var whisper = new WhisperSttService(http, options, NullLogger<WhisperSttService>.Instance, costs);
        var metered = new CountingTranscriber();
        var service = new TieredSpeechToTextService(
            whisper, metered, NullLogger<TieredSpeechToTextService>.Instance);
        return (service, () => metered.Calls);
    }

    /// <summary>16 kHz mono 16-bit PCM WAV with a non-flat signal, so the pre-flight checks pass.</summary>
    private static byte[] Wav(int seconds)
    {
        var samples = 16_000 * seconds;
        var pcm = new byte[samples * 2];
        for (var i = 0; i < samples; i++)
        {
            var value = (short)(short.MaxValue / 4 * Math.Sin(i / 20.0));
            pcm[i * 2] = (byte)(value & 0xFF);
            pcm[(i * 2) + 1] = (byte)((value >> 8) & 0xFF);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcm.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(16_000);
        writer.Write(32_000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcm.Length);
        writer.Write(pcm);
        writer.Flush();
        return stream.ToArray();
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    /// <summary>Stands in for the metered provider; all these tests need is whether it was reached.</summary>
    private sealed class CountingTranscriber : IContextualSpeechToTextService
    {
        public int Calls;

        public Task<SpeechTranscription> TranscribeAsync(
            byte[] audioContent, CancellationToken cancellationToken = default) =>
            TranscribeAsync(audioContent, null!, cancellationToken);

        public Task<SpeechTranscription> TranscribeAsync(
            byte[] audioContent,
            SpeechRecognitionContext context,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(SpeechTranscription.Accepted("metered transcript", 0.95));
        }
    }
}
