using System.Reflection;
using System.Text.Json;
using Application.Learning.Ports;
using Domain.Learning;

namespace Infrastructure.Learning;

/// <summary>
/// Resolves structured <see cref="Recommendation"/>s to verified Uzbek text from an
/// embedded JSON resource (docs/development-guide.md rule 11). The resource holds the recommendation
/// templates plus vetted Uzbek labels for each skill and error category, which fill
/// the <c>{skill}</c> / <c>{category}</c> slots.
/// </summary>
public sealed class JsonRecommendationTemplateProvider : IRecommendationTemplateProvider
{
    private const string ResourceSuffix = "recommendations.json";

    private readonly IReadOnlyDictionary<string, string> _templates;
    private readonly IReadOnlyDictionary<string, string> _skills;
    private readonly IReadOnlyDictionary<string, string> _categories;

    public JsonRecommendationTemplateProvider(
        IReadOnlyDictionary<string, string> templates,
        IReadOnlyDictionary<string, string> skills,
        IReadOnlyDictionary<string, string> categories)
    {
        _templates = templates;
        _skills = skills;
        _categories = categories;
    }

    public static JsonRecommendationTemplateProvider FromEmbeddedResource()
    {
        var assembly = typeof(JsonRecommendationTemplateProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var document = JsonSerializer.Deserialize<ResourceDocument>(stream, options)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' is empty or invalid.");

        return new JsonRecommendationTemplateProvider(
            document.Recommendations,
            document.Skills,
            document.Categories);
    }

    public string? Resolve(Recommendation recommendation)
    {
        if (!_templates.TryGetValue(recommendation.Code, out var template))
            return null;

        if (recommendation.Skill is { } skill)
            template = template.Replace("{skill}", LookupSkill(skill), StringComparison.Ordinal);

        if (recommendation.Category is { } category)
            template = template.Replace("{category}", LookupCategory(category), StringComparison.Ordinal);

        return template;
    }

    private string LookupSkill(SkillType skill) =>
        _skills.TryGetValue(skill.ToString(), out var label) ? label : skill.ToString();

    private string LookupCategory(ErrorCategory category) =>
        _categories.TryGetValue(category.ToString(), out var label) ? label : category.ToString();

    private sealed record ResourceDocument(
        Dictionary<string, string> Recommendations,
        Dictionary<string, string> Skills,
        Dictionary<string, string> Categories);
}
