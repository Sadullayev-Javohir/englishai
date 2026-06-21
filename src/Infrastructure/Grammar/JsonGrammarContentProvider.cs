using System.Text.Json;
using Application.Grammar.Ports;

namespace Infrastructure.Grammar;

/// <summary>
/// Loads vetted Uzbek grammar content (rule explanations and exercise hints) from an
/// embedded JSON resource (docs/development-guide.md rule 11 - Uzbek text comes only from vetted content,
/// never free-generated). Explanation codes and hint codes share one flat map.
/// </summary>
public sealed class JsonGrammarContentProvider : IGrammarContentProvider
{
    private const string ResourceSuffix = "grammar-content.json";

    private readonly IReadOnlyDictionary<string, string> _content;

    public JsonGrammarContentProvider(IReadOnlyDictionary<string, string> content)
    {
        _content = content;
    }

    public static JsonGrammarContentProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonGrammarContentProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var content = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonGrammarContentProvider(content);
    }

    public string? GetExplanation(string? explanationCode) => Lookup(explanationCode);

    public string? GetHint(string? hintCode) => Lookup(hintCode);

    public string? GetMistakeExplanation(string? mistakeCode) =>
        Lookup(string.IsNullOrWhiteSpace(mistakeCode) ? null : $"grammar.mistake.{mistakeCode.Trim()}");

    private string? Lookup(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return _content.TryGetValue(code, out var text) ? text : null;
    }
}
