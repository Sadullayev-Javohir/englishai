using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Speaking.Models;
using DomainSpeaking = Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// Serializable form of <see cref="SynthesizedSpeech"/>. Audio alone is not enough: dropping the
/// viseme sequence would leave the mouth animation dead and dropping the word timings would kill
/// transcript highlighting, so a cached entry either round-trips all four parts or is not cached.
///
/// Ticks rather than <see cref="TimeSpan"/> because System.Text.Json's TimeSpan format has changed
/// between runtimes; an integer tick count cannot drift under a cached payload.
/// </summary>
internal sealed record SynthesizedSpeechEnvelope(
    [property: JsonPropertyName("audio")] string AudioBase64,
    [property: JsonPropertyName("durationTicks")] long DurationTicks,
    [property: JsonPropertyName("frames")] IReadOnlyList<SynthesizedSpeechEnvelope.Frame> Frames,
    [property: JsonPropertyName("animation")] string? Animation,
    [property: JsonPropertyName("isNaturalVoice")] bool IsNaturalVoice,
    [property: JsonPropertyName("timings")] IReadOnlyList<SynthesizedSpeechEnvelope.Timing> Timings)
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal sealed record Frame(
        [property: JsonPropertyName("id")] int VisemeId,
        [property: JsonPropertyName("offsetTicks")] long AudioOffsetTicks);

    internal sealed record Timing(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("textOffset")] int TextOffset,
        [property: JsonPropertyName("wordLength")] int WordLength,
        [property: JsonPropertyName("offsetTicks")] long AudioOffsetTicks,
        [property: JsonPropertyName("durationTicks")] long DurationTicks);

    public static SynthesizedSpeechEnvelope From(SynthesizedSpeech speech) => new(
        Convert.ToBase64String(speech.AudioContent),
        speech.Visemes.AudioDuration.Ticks,
        speech.Visemes.Frames
            .Select(frame => new Frame(frame.VisemeId, frame.AudioOffset.Ticks))
            .ToList(),
        speech.Visemes.Animation,
        speech.IsNaturalVoice,
        speech.Timings
            .Select(timing => new Timing(
                timing.Text,
                timing.TextOffset,
                timing.WordLength,
                timing.AudioOffset.Ticks,
                timing.Duration.Ticks))
            .ToList());

    /// <summary>
    /// Rebuilds the speech, or returns null when the payload cannot produce a valid one. A cache is
    /// never allowed to throw into a learner's request: a corrupt or stale entry must read as a miss
    /// so the caller simply re-synthesizes.
    /// </summary>
    public SynthesizedSpeech? ToSpeech()
    {
        try
        {
            var frames = (Frames ?? Array.Empty<Frame>())
                .Select(frame => new DomainSpeaking.VisemeFrame(
                    frame.VisemeId, TimeSpan.FromTicks(frame.AudioOffsetTicks)))
                .ToList();
            if (frames.Count == 0 || DurationTicks <= 0)
                return null;

            var visemes = DomainSpeaking.VisemeSequence.Create(
                frames, TimeSpan.FromTicks(DurationTicks), Animation);
            var timings = (Timings ?? Array.Empty<Timing>())
                .Select(timing => new SpeechWordTiming(
                    timing.Text,
                    timing.TextOffset,
                    timing.WordLength,
                    TimeSpan.FromTicks(timing.AudioOffsetTicks),
                    TimeSpan.FromTicks(timing.DurationTicks)))
                .ToList();

            return new SynthesizedSpeech(
                Convert.FromBase64String(AudioBase64), visemes, IsNaturalVoice, timings);
        }
        catch
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static SynthesizedSpeech? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SynthesizedSpeechEnvelope>(json, JsonOptions)?.ToSpeech();
        }
        catch
        {
            return null;
        }
    }
}
