namespace Domain.Learning;

/// <summary>
/// A computed, current 0-100 score for one skill, together with how many recent
/// activities backed it (so callers can tell a confident score from a thin one).
/// </summary>
public sealed record SkillScore(SkillType Skill, double Score, int SampleCount);
