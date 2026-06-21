using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Infrastructure.Storage;

public static partial class ObjectKeys
{
    public static string Avatar(string prefix, Guid userId, string checksum) =>
        Build(prefix, "avatars", userId.ToString("N"), checksum, ".webp");

    public static string TopicImage(string prefix, Guid topicId, int slot, string checksum, string contentType) =>
        Build(prefix, "topic-images", topicId.ToString("N"), slot.ToString(), checksum, Extension(contentType));

    public static string ListeningAudio(string prefix, Guid exerciseId, string checksum, string contentType) =>
        Build(prefix, "listening-audio", exerciseId.ToString("N"), checksum, Extension(contentType));

    public static string PlacementAudio(string prefix, Guid questionId, string checksum, string contentType) =>
        Build(prefix, "placement-audio", questionId.ToString("N"), checksum, Extension(contentType));

    public static string Checksum(ReadOnlySpan<byte> data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    public static void Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 512 || key.StartsWith('/') || key.EndsWith('/'))
            throw new InvalidDataException("Object key is invalid.");
        if (key.Contains("..", StringComparison.Ordinal) || key.Contains('\\') || !SafeKey().IsMatch(key))
            throw new InvalidDataException("Object key contains unsafe characters.");
    }

    private static string Build(string prefix, params string[] parts)
    {
        var normalizedPrefix = prefix.Trim('/');
        var key = string.Join('/', new[] { normalizedPrefix }.Concat(parts.Where(part => !string.IsNullOrWhiteSpace(part))));
        Validate(key);
        return key;
    }

    private static string Extension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "audio/wav" => ".wav",
        "audio/mpeg" => ".mp3",
        "audio/ogg" => ".ogg",
        _ => throw new InvalidDataException($"Unsupported media type: {contentType}"),
    };

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9._/-]*$")]
    private static partial Regex SafeKey();
}
