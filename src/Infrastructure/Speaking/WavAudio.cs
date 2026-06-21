using System.Text;

namespace Infrastructure.Speaking;

/// <summary>
/// Minimal WAV helper. The browser records and uploads a 16 kHz / 16-bit / mono PCM
/// WAV, but the Azure Speech push stream (created with its default format) expects
/// <em>raw</em> PCM samples with no RIFF header. This slices out the <c>data</c>
/// chunk so the samples line up. Input that is not a RIFF/WAVE container (e.g.
/// already-raw PCM used in tests) is returned unchanged.
/// </summary>
internal static class WavAudio
{
    private const short PcmFormat = 1;
    private const short RequiredChannels = 1;
    private const int RequiredSampleRate = 16000;
    private const short RequiredBitsPerSample = 16;
    private const double RequiredBytesPerSecond = RequiredSampleRate * RequiredChannels * RequiredBitsPerSample / 8d;

    public static bool TryExtractRequiredPcm(byte[] audio, out byte[] pcm)
    {
        pcm = Array.Empty<byte>();
        if (audio.Length < 44 ||
            audio[0] != 'R' || audio[1] != 'I' || audio[2] != 'F' || audio[3] != 'F' ||
            audio[8] != 'W' || audio[9] != 'A' || audio[10] != 'V' || audio[11] != 'E')
        {
            return false;
        }

        var hasRequiredFormat = false;
        byte[]? data = null;
        var pos = 12;
        while (pos + 8 <= audio.Length)
        {
            var id = Encoding.ASCII.GetString(audio, pos, 4);
            var size = BitConverter.ToInt32(audio, pos + 4);
            var dataStart = pos + 8;
            if (size < 0 || dataStart > audio.Length - size)
                return false;

            if (id == "fmt " && size >= 16)
            {
                hasRequiredFormat =
                    BitConverter.ToInt16(audio, dataStart) == PcmFormat &&
                    BitConverter.ToInt16(audio, dataStart + 2) == RequiredChannels &&
                    BitConverter.ToInt32(audio, dataStart + 4) == RequiredSampleRate &&
                    BitConverter.ToInt16(audio, dataStart + 14) == RequiredBitsPerSample;
            }
            else if (id == "data")
            {
                data = new byte[size];
                Array.Copy(audio, dataStart, data, 0, size);
            }

            pos = dataStart + size + (size & 1);
        }

        if (!hasRequiredFormat || data is null || data.Length < 2 || data.Length % 2 != 0)
            return false;

        pcm = data;
        return true;
    }

    public static bool HasAudibleSignal(byte[] pcm)
    {
        if (pcm.Length < 2)
            return false;

        short minimum = short.MaxValue;
        short maximum = short.MinValue;
        for (var index = 0; index < pcm.Length; index += 2)
        {
            var sample = BitConverter.ToInt16(pcm, index);
            minimum = Math.Min(minimum, sample);
            maximum = Math.Max(maximum, sample);
        }

        // Reject only effectively flat digital silence. Quiet real microphones must still reach
        // Azure, which owns the actual speech/no-speech and confidence decision.
        return maximum - minimum >= 16;
    }

    public static byte[] ExtractPcm(byte[] audio)
    {
        // Header layout: "RIFF" <size> "WAVE" then a series of <id><size><bytes> chunks.
        if (audio.Length < 12 ||
            audio[0] != 'R' || audio[1] != 'I' || audio[2] != 'F' || audio[3] != 'F' ||
            audio[8] != 'W' || audio[9] != 'A' || audio[10] != 'V' || audio[11] != 'E')
        {
            return audio;
        }

        var pos = 12;
        while (pos + 8 <= audio.Length)
        {
            var id = Encoding.ASCII.GetString(audio, pos, 4);
            var size = BitConverter.ToInt32(audio, pos + 4);
            var dataStart = pos + 8;

            if (id == "data")
            {
                var length = Math.Clamp(size, 0, audio.Length - dataStart);
                var pcm = new byte[length];
                Array.Copy(audio, dataStart, pcm, 0, length);
                return pcm;
            }

            // Chunks are word-aligned: an odd size is followed by a padding byte.
            pos = dataStart + size + (size & 1);
        }

        return audio;
    }

    public static double DurationSeconds(byte[] pcm) =>
        Math.Max(0, pcm.LongLength / RequiredBytesPerSecond);
}
