using System.Text.Json;
using Application.Writing.Ports;

namespace Infrastructure.Writing;

/// <summary>
/// Loads vetted Uzbek writing-feedback content (issue-code explanations) from an embedded
/// JSON resource (docs/development-guide.md rule 11 - Uzbek text comes only from vetted content, never
/// free-generated). The assessor returns structured issue codes; this maps them to Uzbek.
/// </summary>
public sealed class JsonWritingContentProvider : IWritingContentProvider
{
    private const string ResourceSuffix = "writing-feedback.json";

    private readonly IReadOnlyDictionary<string, string> _explanations;

    public JsonWritingContentProvider(IReadOnlyDictionary<string, string> explanations)
    {
        _explanations = explanations;
    }

    public static JsonWritingContentProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonWritingContentProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var explanations = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonWritingContentProvider(explanations);
    }

    public string? GetIssueExplanation(string? issueCode)
    {
        if (string.IsNullOrWhiteSpace(issueCode))
            return null;

        return _explanations.TryGetValue(issueCode, out var text) ? text : null;
    }
}
