using System.Text.Json;
using Application.Notifications.Ports;

namespace Infrastructure.Notifications;

/// <summary>
/// Loads verified Uzbek notification templates from an embedded JSON resource and fills
/// <c>{placeholder}</c> tokens from the supplied args (docs/development-guide.md rule 11 - Uzbek text
/// comes only from vetted templates, never free-generated).
/// </summary>
public sealed class JsonNotificationTemplateProvider : INotificationTemplateProvider
{
    private const string ResourceSuffix = "notifications.json";

    private readonly IReadOnlyDictionary<string, string> _templates;

    public JsonNotificationTemplateProvider(IReadOnlyDictionary<string, string> templates)
    {
        _templates = templates;
    }

    public static JsonNotificationTemplateProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonNotificationTemplateProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var templates = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();

        return new JsonNotificationTemplateProvider(templates);
    }

    public string? Get(string code, IReadOnlyDictionary<string, string>? args = null)
    {
        if (!_templates.TryGetValue(code, out var template))
            return null;

        if (args is null)
            return template;

        foreach (var (key, value) in args)
            template = template.Replace("{" + key + "}", value, StringComparison.Ordinal);

        return template;
    }
}
