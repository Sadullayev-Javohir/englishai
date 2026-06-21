namespace Infrastructure.Storage;

public static class MediaValidation
{
    public const long MaxAvatarBytes = 5 * 1024 * 1024;
    public const long MaxTopicImageBytes = 12 * 1024 * 1024;
    public const long MaxGeneratedAudioBytes = 25 * 1024 * 1024;

    public static string DetectImage(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return "image/jpeg";
        if (data.Length >= 8 && data[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        throw new InvalidDataException("Unsupported or invalid image content.");
    }

    public static string DetectAudio(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WAVE"u8)) return "audio/wav";
        if (data.Length >= 3 && data[..3].SequenceEqual("ID3"u8)) return "audio/mpeg";
        if (data.Length >= 2 && data[0] == 0xFF && (data[1] & 0xE0) == 0xE0) return "audio/mpeg";
        if (data.Length >= 4 && data[..4].SequenceEqual("OggS"u8)) return "audio/ogg";
        throw new InvalidDataException("Unsupported or invalid audio content.");
    }

    public static void EnsureSize(long sizeBytes, long maxBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > maxBytes)
            throw new InvalidDataException($"Media must be between 1 and {maxBytes} bytes.");
    }
}
