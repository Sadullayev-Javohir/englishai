using Application.Ai;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Domain.Speaking;
using FluentAssertions;
using Infrastructure.Speaking;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// A cached synthesis must be indistinguishable from a fresh one. Audio alone would leave the mouth
/// animation dead and the transcript unhighlighted, so every part of the payload is asserted.
/// </summary>
public class SynthesizedSpeechCacheTests
{
    private const string Voice = "en-GB-SoniaNeural";
    private const string Text = "Hello! What did you do today?";

    private static SynthesizedSpeech Sample() => new(
        new byte[] { 1, 2, 3, 4, 5 },
        VisemeSequence.Create(
            new[]
            {
                new VisemeFrame(0, TimeSpan.Zero),
                new VisemeFrame(7, TimeSpan.FromMilliseconds(120)),
            },
            TimeSpan.FromMilliseconds(400),
            "<svg id=\"redlips\"><animate/></svg>"),
        IsNaturalVoice: false,
        WordTimings: new[]
        {
            new SpeechWordTiming("Hello", 0, 5, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(90)),
        });

    [Fact]
    public async Task Cache_round_trips_audio_visemes_animation_and_word_timings()
    {
        var cache = new InMemorySynthesizedSpeechCache();
        var original = Sample();

        await cache.SetAsync(Voice, Text, original);
        var restored = await cache.GetAsync(Voice, Text);

        restored.Should().NotBeNull();
        restored!.AudioContent.Should().Equal(original.AudioContent);
        restored.IsNaturalVoice.Should().BeFalse();
        restored.Visemes.AudioDuration.Should().Be(original.Visemes.AudioDuration);
        restored.Visemes.Animation.Should().Be(original.Visemes.Animation);
        restored.Visemes.Frames.Should().BeEquivalentTo(original.Visemes.Frames);
        restored.Timings.Should().BeEquivalentTo(original.Timings);
    }

    [Fact]
    public void Json_envelope_round_trips_every_field()
    {
        // The shared (Redis) cache stores JSON, so the envelope is the part that can silently lose
        // data. Assert it directly rather than only through the in-memory tier.
        var original = Sample();

        var restored = SynthesizedSpeechEnvelope.FromJson(SynthesizedSpeechEnvelope.From(original).ToJson());

        restored.Should().NotBeNull();
        restored!.AudioContent.Should().Equal(original.AudioContent);
        restored.IsNaturalVoice.Should().BeFalse();
        restored.Visemes.Animation.Should().Be(original.Visemes.Animation);
        restored.Visemes.Frames.Should().BeEquivalentTo(original.Visemes.Frames);
        restored.Timings.Should().BeEquivalentTo(original.Timings);
    }

    [Fact]
    public static void A_corrupt_payload_reads_as_a_miss_rather_than_throwing()
    {
        SynthesizedSpeechEnvelope.FromJson("{\"audio\":\"not-base64!!\",\"durationTicks\":0}")
            .Should().BeNull();
        SynthesizedSpeechEnvelope.FromJson("not json at all").Should().BeNull();
    }

    [Fact]
    public async Task Different_voices_do_not_share_an_entry()
    {
        // The same sentence in another accent tutor's voice is different audio.
        var cache = new InMemorySynthesizedSpeechCache();

        await cache.SetAsync(Voice, Text, Sample());

        (await cache.GetAsync("en-US-JennyNeural", Text)).Should().BeNull();
    }

    [Fact]
    public async Task A_hit_skips_synthesis_and_is_billed_at_zero()
    {
        var inner = Substitute.For<IVoicedTextToSpeechService>();
        inner.SynthesizeWithVoiceAsync(Text, Voice, Arg.Any<CancellationToken>()).Returns(Sample());
        var costs = Substitute.For<IVariableCostMeter>();
        var service = new CachedTextToSpeechService(
            inner, new InMemorySynthesizedSpeechCache(), new AzureSpeechOptions { VoiceName = Voice }, costs);

        await service.SynthesizeWithVoiceAsync(Text, Voice, CancellationToken.None);
        await service.SynthesizeWithVoiceAsync(Text, Voice, CancellationToken.None);

        await inner.Received(1).SynthesizeWithVoiceAsync(Text, Voice, Arg.Any<CancellationToken>());
        // The hit stays visible on the cost dashboard, but must never be billed.
        costs.Received(1).Record(
            Arg.Is(VariableCostCategory.TextToSpeechCached),
            Arg.Any<double>(),
            Arg.Is("character"),
            Arg.Is(0d),
            Arg.Any<string?>(),
            Arg.Any<AiFeature>(),
            Arg.Any<string?>());
        costs.DidNotReceive().Record(
            Arg.Is(VariableCostCategory.TextToSpeech),
            Arg.Any<double>(),
            Arg.Any<string>(),
            Arg.Any<double>(),
            Arg.Any<string?>(),
            Arg.Any<AiFeature>(),
            Arg.Any<string?>());
    }

    [Fact]
    public async Task Long_text_is_not_cached_because_it_never_repeats()
    {
        var cache = new InMemorySynthesizedSpeechCache();
        var longText = new string('a', SynthesizedSpeechCacheKey.MaxCacheableTextLength + 1);

        await cache.SetAsync(Voice, longText, Sample());

        (await cache.GetAsync(Voice, longText)).Should().BeNull();
    }
}
