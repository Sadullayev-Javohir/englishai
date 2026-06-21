using System.Text.Json;
using Application.Listening.Ports;

namespace Infrastructure.Listening;

/// <summary>
/// Loads vetted Uzbek listening content (quiz hints) from an embedded JSON resource
/// (docs/development-guide.md rule 11 - Uzbek text comes only from vetted content, never free-generated).
/// </summary>
public sealed class JsonListeningContentProvider : IListeningContentProvider
{
    private const string ResourceSuffix = "listening-hints.json";

    private readonly IReadOnlyDictionary<string, string> _hints;

    public JsonListeningContentProvider(IReadOnlyDictionary<string, string> hints)
    {
        _hints = hints;
    }

    public static JsonListeningContentProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonListeningContentProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var hints = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonListeningContentProvider(hints);
    }

    public string? GetHint(string? hintCode)
    {
        if (string.IsNullOrWhiteSpace(hintCode))
            return null;

        return _hints.TryGetValue(hintCode, out var hint) ? hint : null;
    }
}
