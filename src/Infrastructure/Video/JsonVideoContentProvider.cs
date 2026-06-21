using System.Text.Json;
using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Loads vetted Uzbek video content (quiz hints) from an embedded JSON resource
/// (docs/development-guide.md rule 11 - Uzbek text comes only from vetted content, never free-generated).
/// </summary>
public sealed class JsonVideoContentProvider : IVideoContentProvider
{
    private const string ResourceSuffix = "video-hints.json";

    private readonly IReadOnlyDictionary<string, string> _hints;

    public JsonVideoContentProvider(IReadOnlyDictionary<string, string> hints)
    {
        _hints = hints;
    }

    public static JsonVideoContentProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonVideoContentProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var hints = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonVideoContentProvider(hints);
    }

    public string? GetHint(string? hintCode)
    {
        if (string.IsNullOrWhiteSpace(hintCode))
            return null;

        return _hints.TryGetValue(hintCode, out var hint) ? hint : null;
    }
}
