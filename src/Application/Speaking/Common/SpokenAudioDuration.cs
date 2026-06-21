using System.Text;

namespace Application.Speaking.Common;

/// <summary>
/// How long a learner's uploaded utterance is, in seconds - the quantity Azure bills speech-to-text
/// on, and therefore the quantity the daily speaking allowance is metered in.
///
/// Duplicated (deliberately) from the Infrastructure WAV helper rather than shared: Application must
/// not depend on Infrastructure, and this needs no knowledge of the speech provider - only of the
/// 16 kHz / 16-bit / mono PCM WAV the browser uploads.
/// </summary>
public static class SpokenAudioDuration
{
    private const int SampleRate = 16_000;
    private const int Channels = 1;
    private const int BitsPerSample = 16;
    private const double BytesPerSecond = SampleRate * Channels * BitsPerSample / 8d;
    private const int MinimumRiffLength = 44;

    public static double Seconds(byte[]? audio)
    {
        if (audio is null || audio.Length == 0)
            return 0;

        var payloadLength = TryFindDataChunkLength(audio) ?? audio.LongLength;
        return Math.Max(0, payloadLength / BytesPerSecond);
    }

    public static double Minutes(byte[]? audio) => Seconds(audio) / 60d;

    /// <summary>
    /// Length of the RIFF <c>data</c> chunk, or null when the payload is not a WAV container (raw
    /// PCM, as some tests and clients send). Returning null makes the caller fall back to the whole
    /// buffer, which is the correct reading for headerless PCM.
    /// </summary>
    private static long? TryFindDataChunkLength(byte[] audio)
    {
        if (audio.Length < MinimumRiffLength ||
            audio[0] != 'R' || audio[1] != 'I' || audio[2] != 'F' || audio[3] != 'F' ||
            audio[8] != 'W' || audio[9] != 'A' || audio[10] != 'V' || audio[11] != 'E')
        {
            return null;
        }

        var position = 12;
        while (position + 8 <= audio.Length)
        {
            var id = Encoding.ASCII.GetString(audio, position, 4);
            var size = BitConverter.ToInt32(audio, position + 4);
            var dataStart = position + 8;
            if (size < 0 || dataStart > audio.Length)
                return null;

            if (id == "data")
                return Math.Clamp(size, 0, audio.Length - dataStart);

            // Chunks are word-aligned: an odd size is followed by a padding byte.
            position = dataStart + size + (size & 1);
        }

        return null;
    }
}
