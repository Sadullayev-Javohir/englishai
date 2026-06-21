using Domain.Learning;

namespace Application.Learning.Ports;

/// <summary>
/// Turns a structured <see cref="Recommendation"/> into verified Uzbek text
/// (docs/development-guide.md rule 11 - wording comes only from vetted templates, never generated).
/// The implementation fills the template's skill/category slots with their own
/// vetted Uzbek labels.
/// </summary>
public interface IRecommendationTemplateProvider
{
    /// <summary>Returns the resolved Uzbek text, or <c>null</c> if the code is unknown.</summary>
    string? Resolve(Recommendation recommendation);
}
