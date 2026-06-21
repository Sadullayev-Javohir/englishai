using System.Reflection;
using System.Text.Json;
using Application.Speaking.Ports;

namespace Infrastructure.Speaking;

/// <summary>
/// Loads verified Uzbek example sentences keyed by English word from an embedded
/// JSON resource. Words not present in the curated map return null, so the caller
/// renders the example card conditionally (docs/development-guide.md rule 11 - never free-generate
/// Uzbek; only hand-vetted template content is used).
/// </summary>
public sealed class JsonWordExampleProvider : IWordExampleProvider
{
    private const string ResourceSuffix = "wordExamples.json";

    private readonly IReadOnlyDictionary<string, string> _examples;

    public JsonWordExampleProvider(IReadOnlyDictionary<string, string> examples)
    {
        _examples = examples;
    }

    public static JsonWordExampleProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonWordExampleProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var examples = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonWordExampleProvider(examples);
    }

    public string? GetExampleSentenceUz(string word)
    {
        if (word is null)
            return null;

        var normalized = word.Trim().ToLowerInvariant();
        return _examples.GetValueOrDefault(normalized);
    }
}
