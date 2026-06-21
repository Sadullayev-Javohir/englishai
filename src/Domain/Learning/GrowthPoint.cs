namespace Domain.Learning;

/// <summary>
/// One point on a skill's growth curve: the decay-weighted score for a skill as of
/// the end of a given week. A series of these powers the progress-dashboard charts
/// (PROJECT-SPEC Faza 2 "o'sish grafiklari").
/// </summary>
public sealed record GrowthPoint(DateTimeOffset WeekEnding, SkillType Skill, double Score);
