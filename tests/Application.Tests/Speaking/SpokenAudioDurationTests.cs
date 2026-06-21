using System.Text;
using Application.Speaking.Common;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Speaking;

/// <summary>
/// The daily speaking allowance is metered on this number, so it has to match what the speech
/// provider actually bills: the length of the learner's audio, not the size of the upload.
/// </summary>
public class SpokenAudioDurationTests
{
    private const int BytesPerSecond = 16_000 * 2;

    private static byte[] Wav(int pcmBytes)
    {
        var pcm = new byte[pcmBytes];
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcmBytes);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(16_000);
        writer.Write(BytesPerSecond);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcmBytes);
        writer.Write(pcm);
        writer.Flush();
        return stream.ToArray();
    }

    [Fact]
    public void A_wav_is_measured_from_its_data_chunk_not_its_total_size()
    {
        // The 44-byte header must not be billed as audio.
        SpokenAudioDuration.Seconds(Wav(BytesPerSecond * 3)).Should().BeApproximately(3, 0.001);
    }

    [Fact]
    public void Minutes_is_seconds_over_sixty()
    {
        SpokenAudioDuration.Minutes(Wav(BytesPerSecond * 30)).Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void Headerless_pcm_falls_back_to_the_whole_buffer()
    {
        // Some clients and tests upload raw samples; treating them as zero-length would hand out
        // unmetered speech.
        SpokenAudioDuration.Seconds(new byte[BytesPerSecond * 2]).Should().BeApproximately(2, 0.001);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    public void Missing_audio_is_zero_rather_than_an_exception(byte[]? audio)
    {
        SpokenAudioDuration.Seconds(audio).Should().Be(0);
    }

    [Fact]
    public void A_truncated_wav_does_not_over_report()
    {
        // A header claiming more data than the file contains must not bill the difference.
        var wav = Wav(BytesPerSecond);
        var truncated = wav.Take(wav.Length / 2).ToArray();

        SpokenAudioDuration.Seconds(truncated).Should().BeLessThan(1);
    }
}
