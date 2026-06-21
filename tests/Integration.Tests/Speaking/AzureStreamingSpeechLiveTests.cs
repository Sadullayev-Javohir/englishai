using Application.Ai;
using Application.Speaking.Ports;
using FluentAssertions;
using Infrastructure.Speaking;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// End-to-end check of the fixed streaming session against the REAL Azure Speech service.
/// Gated on AZURE_SPEECH_KEY/AZURE_SPEECH_REGION so it is skipped in CI (and anywhere the
/// credentials are absent). Run locally with:
///   AZURE_SPEECH_KEY=... AZURE_SPEECH_REGION=eastus \
///   dotnet test tests/Integration.Tests --filter FullyQualifiedName~AzureStreamingSpeechLiveTests
/// It reproduces exactly what the accent-tutor hub does: create a session, stream 16 kHz mono
/// PCM in chunks, then CompleteAsync - and asserts a transcript comes back instead of NoSpeech.
/// </summary>
public class AzureStreamingSpeechLiveTests
{
    private static (string Key, string Region)? Credentials()
    {
        // Require an explicit opt-in so this never runs (and never bills Azure) in a CI that
        // happens to hold Speech secrets. Run with RUN_AZURE_LIVE_TESTS=1 plus the credentials.
        if (Environment.GetEnvironmentVariable("RUN_AZURE_LIVE_TESTS") != "1")
            return null;
        var key = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
        var region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
        return string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(region)
            ? null
            : (key!, region!);
    }

    [Fact]
    public async Task Streamed_speech_produces_a_transcript_end_to_end()
    {
        var credentials = Credentials();
        if (credentials is null) return; // No Azure credentials (e.g. CI) - live test no-ops.
        var (key, region) = credentials.Value;

        const string sentence = "I like playing football with my friends on the weekend.";
        var pcm = await SynthesizePcm16kMonoAsync(key, region, sentence);
        pcm.Length.Should().BeGreaterThan(16_000, "at least ~0.5s of 16 kHz mono PCM should be synthesized");

        var options = new AzureSpeechOptions { Key = key, Region = region };
        var service = new AzureSpeechToTextService(
            options,
            NullLogger<AzureSpeechToTextService>.Instance,
            Substitute.For<IVariableCostMeter>());
        var context = new SpeechRecognitionContext(
            "What do you like doing on the weekend?",
            "free conversation",
            Array.Empty<string>(),
            Array.Empty<string>(),
            IncludeNameHints: false);

        var partials = new List<string>();
        var session = service.CreateSession(context, text => { partials.Add(text); return Task.CompletedTask; });

        await session.StartAsync();
        // Stream in 100ms (3200-byte) chunks, mirroring the hub's bounded AppendAudioChunk, then
        // complete right after the last chunk - the exact timing that used to drop the final result.
        const int chunkBytes = 3200;
        for (var offset = 0; offset < pcm.Length; offset += chunkBytes)
            await session.WriteAsync(pcm.AsMemory(offset, Math.Min(chunkBytes, pcm.Length - offset)));
        var transcription = await session.CompleteAsync();

        transcription.IsAccepted.Should().BeTrue(
            "the fixed session must return a transcript (final or partial fallback), not NoSpeech");
        transcription.Text.ToLowerInvariant().Should().Contain("football");
    }

    private static async Task<byte[]> SynthesizePcm16kMonoAsync(string key, string region, string text)
    {
        var config = SpeechConfig.FromSubscription(key, region);
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);
        config.SpeechSynthesisVoiceName = "en-US-JennyNeural";
        using var synthesizer = new SpeechSynthesizer(config, audioConfig: null);
        using var result = await synthesizer.SpeakTextAsync(text);
        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            throw new InvalidOperationException($"TTS failed: {result.Reason}");
        return result.AudioData;
    }
}
