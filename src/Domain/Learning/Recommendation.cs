namespace Domain.Learning;

/// <summary>
/// A single structured "what to do next" suggestion. It carries only codes and
/// references (docs/development-guide.md rule 11) - the Uzbek wording is resolved from templates in
/// the Application/Infrastructure layers, never produced here. <see cref="Code"/>
/// keys the template; <see cref="Skill"/> / <see cref="Category"/> fill its slots.
/// </summary>
public sealed record Recommendation(
    string Code,
    SkillType? Skill = null,
    ErrorCategory? Category = null);
