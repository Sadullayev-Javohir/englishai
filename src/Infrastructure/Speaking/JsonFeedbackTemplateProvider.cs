using System.Reflection;
using System.Text.Json;
using Application.Speaking.Ports;

namespace Infrastructure.Speaking;

/// <summary>
/// Loads verified Uzbek feedback templates from an embedded JSON resource and fills
/// <c>{placeholder}</c> tokens from the supplied args (docs/development-guide.md rule 11 - Uzbek
/// text comes only from vetted templates, never free-generated).
/// </summary>
public sealed class JsonFeedbackTemplateProvider : IFeedbackTemplateProvider
{
    private const string ResourceSuffix = "speaking-feedback.json";

    private readonly IReadOnlyDictionary<string, string> _templates;

    public JsonFeedbackTemplateProvider(IReadOnlyDictionary<string, string> templates)
    {
        _templates = templates;
    }

    public static JsonFeedbackTemplateProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonFeedbackTemplateProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var templates = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonFeedbackTemplateProvider(templates);
    }

    public string? Get(string code, IReadOnlyDictionary<string, string>? args = null)
    {
        var template = Resolve(code, args);
        if (template is null)
            return null;

        if (args is null)
            return template;

        foreach (var (key, value) in args)
            template = template.Replace("{" + key + "}", value, StringComparison.Ordinal);

        return template;
    }

    /// <summary>
    /// Returns the template for a code, choosing between numbered variants when they exist:
    /// <c>pron.mispronunciation</c> may be backed by <c>pron.mispronunciation.1</c>,
    /// <c>.2</c>, … The same phrase repeated after every single turn reads like a broken machine,
    /// and a learner who hears it enough times stops reading the feedback at all.
    ///
    /// The choice is DETERMINISTIC in the arguments (usually the word being corrected), not random:
    /// the same word always gets the same wording, so a learner is not told the same mistake three
    /// different ways, and tests stay reproducible.
    /// </summary>
    private string? Resolve(string code, IReadOnlyDictionary<string, string>? args)
    {
        if (_templates.TryGetValue(code, out var exact))
            return exact;

        var variants = new List<string>();
        for (var index = 1; _templates.TryGetValue($"{code}.{index}", out var variant); index++)
            variants.Add(variant);

        if (variants.Count == 0)
            return null;

        return variants[VariantIndex(args, variants.Count)];
    }

    private static int VariantIndex(IReadOnlyDictionary<string, string>? args, int count)
    {
        if (args is null || args.Count == 0)
            return 0;

        // Ordinal hash of the argument values, folded to a positive index. Deliberately hand-rolled:
        // string.GetHashCode is randomized per process, which would make the wording change on every
        // restart and every test run.
        var hash = 17;
        foreach (var value in args.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value))
        {
            foreach (var character in value)
                hash = unchecked((hash * 31) + char.ToLowerInvariant(character));
        }

        return Math.Abs(hash % count);
    }
}
