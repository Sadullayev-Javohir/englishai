using Domain.Assessment;
using Domain.Learning;

namespace Domain.Levels;

/// <summary>
/// A single "can-do" statement for one CEFR level and one skill (PROJECT-SPEC M.1),
/// based on the Council of Europe CEFR Global Scale. The English statement is the
/// canonical reference (and drives the Level Exit Test design, M.6); the Uzbek text
/// shown to the learner is a vetted template resolved from the content layer by
/// <see cref="StatementCode"/> (rule 11 - no free-form Uzbek).
/// </summary>
public sealed record CefrLevelDescriptor(CefrLevel Level, SkillType Skill, string StatementEn)
{
    /// <summary>
    /// The content-store key for the vetted Uzbek statement, e.g. "level.a1.speaking".
    /// </summary>
    public string StatementCode => $"level.{Level.ToString().ToLowerInvariant()}.{Skill.ToString().ToLowerInvariant()}";
}
