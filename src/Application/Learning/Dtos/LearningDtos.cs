using Domain.Assessment;
using Domain.Learning;

namespace Application.Learning.Dtos;

public sealed record SkillScoreDto(SkillType Skill, double Score, int SampleCount)
{
    public static SkillScoreDto FromDomain(SkillScore score) =>
        new(score.Skill, score.Score, score.SampleCount);
}

public sealed record ErrorHeatmapEntryDto(ErrorCategory Category, int Count);

public sealed record LevelStatusDto(
    bool IsEligible,
    int MasteredSkillCount,
    int RequiredMasteredSkills,
    bool ConfirmationTestPassed,
    bool AtMaxLevel)
{
    public static LevelStatusDto FromDomain(LevelTransitionStatus status) =>
        new(status.IsEligible, status.MasteredSkillCount, status.RequiredMasteredSkills,
            status.ConfirmationTestPassed, status.AtMaxLevel);
}

/// <summary>Overview for the progress dashboard (PROJECT-SPEC Faza 2).</summary>
public sealed record LearnerOverviewDto(
    Guid LearnerId,
    CefrLevel OverallLevel,
    IReadOnlyList<SkillScoreDto> SkillScores,
    IReadOnlyList<ErrorHeatmapEntryDto> ErrorHeatmap,
    LevelStatusDto LevelStatus)
{
    public static LearnerOverviewDto FromDomain(LearnerProfile profile, DateTimeOffset now) =>
        new(
            profile.LearnerId,
            profile.OverallLevel,
            profile.SkillScores(now).Select(SkillScoreDto.FromDomain).ToList(),
            profile.ErrorHeatmap(now)
                .OrderByDescending(e => e.Value).ThenBy(e => e.Key)
                .Select(e => new ErrorHeatmapEntryDto(e.Key, e.Value))
                .ToList(),
            LevelStatusDto.FromDomain(profile.LevelStatus(now)));
}

/// <summary>A recommendation with its structured code and resolved Uzbek text.</summary>
public sealed record RecommendationDto(
    string Code,
    string? Text,
    SkillType? Skill,
    ErrorCategory? Category);

public sealed record GrowthPointDto(DateTimeOffset WeekEnding, SkillType Skill, double Score)
{
    public static GrowthPointDto FromDomain(GrowthPoint point) =>
        new(point.WeekEnding, point.Skill, point.Score);
}

/// <summary>Result of recording a confirmation mini-test (PROJECT-SPEC G.4).</summary>
public sealed record ConfirmationTestResultDto(CefrLevel OverallLevel, bool LevelAdvanced);
