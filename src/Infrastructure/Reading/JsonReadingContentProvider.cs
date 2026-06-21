using System.Text.Json;
using Application.Reading.Ports;

namespace Infrastructure.Reading;

/// <summary>
/// Loads vetted Uzbek reading content (quiz hints) from an embedded JSON resource
/// (docs/development-guide.md rule 11 - Uzbek text comes only from vetted content, never free-generated).
/// </summary>
public sealed class JsonReadingContentProvider : IReadingContentProvider
{
    private const string ResourceSuffix = "reading-hints.json";

    private readonly IReadOnlyDictionary<string, string> _hints;

    public JsonReadingContentProvider(IReadOnlyDictionary<string, string> hints)
    {
        _hints = hints;
    }

    public static JsonReadingContentProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonReadingContentProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var hints = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonReadingContentProvider(hints);
    }

    public string? GetHint(string? hintCode)
    {
        if (string.IsNullOrWhiteSpace(hintCode))
            return null;

        return _hints.TryGetValue(hintCode, out var hint) ? hint : null;
    }
}
