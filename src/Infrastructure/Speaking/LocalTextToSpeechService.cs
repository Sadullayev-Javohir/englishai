using Application.Speaking.Models;
using Application.Speaking.Ports;
using Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// Deterministic local stand-in for Azure Neural TTS. Produces a viseme track and technical
/// fallback WAV, marked as non-natural so pronunciation clients use an installed browser voice.
/// The sequence carries one
/// placeholder red-lips SVG for the whole utterance (the same single-inject-and-trigger
/// contract the Azure adapter's <c>redlips_front</c> output uses) so the SVG renderer works
/// offline. Production uses the Azure adapter and caches repeated phrases (docs/development-guide.md rule 10).
/// </summary>
public sealed class LocalTextToSpeechService : ITextToSpeechService
{
    private const int MillisecondsPerCharacter = 70;
    private const int VisemeIdCount = 22; // Azure viseme ids are 0..21

    public Task<SynthesizedSpeech> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        var characters = text.ToCharArray();
        var frames = new List<VisemeFrame>();

        for (var i = 0; i < characters.Length; i++)
        {
            var visemeId = char.IsWhiteSpace(characters[i])
                ? 0
                : Math.Abs(characters[i]) % VisemeIdCount;
            frames.Add(new VisemeFrame(visemeId, TimeSpan.FromMilliseconds(i * MillisecondsPerCharacter)));
        }

        if (frames.Count == 0)
            frames.Add(new VisemeFrame(0, TimeSpan.Zero));

        var duration = TimeSpan.FromMilliseconds(Math.Max(characters.Length, 1) * MillisecondsPerCharacter);
        // One complete SVG for the whole utterance - the same single-inject-and-play contract
        // the Azure redlips_front adapter produces, so the client renderer works offline too.
        var visemes = VisemeSequence.Create(frames, duration, PlaceholderSvg(frames));
        return Task.FromResult(new SynthesizedSpeech(
            CreateFallbackWav(duration),
            visemes,
            false,
            BuildWordTimings(text)));
    }

    private static IReadOnlyList<SpeechWordTiming> BuildWordTimings(string text)
    {
        var timings = new List<SpeechWordTiming>();
        foreach (System.Text.RegularExpressions.Match match in
                 System.Text.RegularExpressions.Regex.Matches(text, @"\S+"))
        {
            timings.Add(new SpeechWordTiming(
                match.Value,
                match.Index,
                match.Length,
                TimeSpan.FromMilliseconds(match.Index * MillisecondsPerCharacter),
                TimeSpan.FromMilliseconds(Math.Max(match.Length, 1) * MillisecondsPerCharacter)));
        }

        return timings;
    }

    private static byte[] CreateFallbackWav(TimeSpan duration)
    {
        const int sampleRate = 16000;
        const short channels = 1;
        const short bitsPerSample = 16;
        var sampleCount = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds * sampleRate));
        var dataSize = sampleCount * sizeof(short);
        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        for (var i = 0; i < sampleCount; i++)
        {
            var envelope = Math.Sin(Math.PI * (i % 1120) / 1120.0);
            var sample = (short)(Math.Sin(2 * Math.PI * 220 * i / sampleRate) * envelope * short.MaxValue * 0.08);
            writer.Write(sample);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// A minimal front-view red-lips SVG that animates the mouth through the whole utterance:
    /// a single ellipse whose vertical opening (<c>ry</c>) steps through one value per viseme
    /// via SMIL <c>values</c>/<c>keyTimes</c>. The first (only) animation carries
    /// <c>begin="0.5s"</c> so the client's swap-and-trigger path (replace <c>0.5s</c> →
    /// <c>indefinite</c>, then <c>beginElement()</c>) fires it - mirroring the Azure
    /// <c>redlips_front</c> single-SVG contract offline.
    /// </summary>
    private static string PlaceholderSvg(IReadOnlyList<VisemeFrame> frames)
    {
        // Map each viseme id to a mouth opening (ry), bracketed by a closed mouth at the start
        // and end so the SMIL `values` list always has >= 2 entries (valid even for one frame)
        // and the mouth opens from / settles back to neutral.
        var openings = new List<int> { 6 };
        openings.AddRange(frames.Select(f => 4 + f.VisemeId % 11 * 2)); // 4..24 vertical opening
        openings.Add(6);
        var values = string.Join(";", openings);
        var last = openings.Count - 1;
        var keyTimes = string.Join(";", openings.Select((_, i) => (i / (double)last).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)));
        var totalSeconds = (frames.Count * MillisecondsPerCharacter / 1000.0)
            .ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        return
            "<svg viewBox=\"0 0 100 100\" xmlns=\"http://www.w3.org/2000/svg\">" +
            "<ellipse cx=\"50\" cy=\"50\" rx=\"26\" ry=\"6\" fill=\"#C8645A\" stroke=\"#A84E45\" stroke-width=\"2\">" +
            $"<animate attributeName=\"ry\" begin=\"0.5s\" dur=\"{totalSeconds}s\" fill=\"freeze\" " +
            $"values=\"{values}\" keyTimes=\"{keyTimes}\"/>" +
            "</ellipse></svg>";
    }
}
