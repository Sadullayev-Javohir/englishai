using Application.Analytics.Dtos;
using Application.Gamification.Dtos;
using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;

namespace Application.Learning.Dtos;

public enum ProgressDataConfidence { Baseline = 0, Low = 1, Medium = 2, High = 3 }
public enum ProgressInsightSource { Hermes = 0, Local = 1 }

public sealed record ProgressSkillSnapshotDto(
    SkillType Skill, double Score, int SampleCount, ProgressDataConfidence Confidence,
    bool IsPlacementBaseline, double EightWeekDelta, IReadOnlyList<GrowthPointDto> Last8Weeks);

public sealed record ProgressErrorDetailDto(
    Guid Id,
    ErrorCategory Category,
    SkillType Skill,
    DateTimeOffset OccurredAt,
    string? Source,
    Guid? SourceId,
    string? Prompt,
    string? LearnerAnswer,
    string? ExpectedAnswer,
    string? Explanation);

public sealed record ProgressErrorSnapshotDto(
    ErrorCategory Category,
    int CountLast30Days,
    IReadOnlyList<ProgressErrorDetailDto> RecentExamples);
public sealed record VocabularyStageCountDto(ReviewStage Stage, int Count);
public sealed record TopicProgressSummaryDto(int StartedTopics, int MasteredTopics, int PassedModules);

public sealed record VocabularySummaryDto(
    int Total, int Learning, int Mastered, int Due, int AddedLast7Days, int AddedLast30Days,
    int TotalFailCount, IReadOnlyList<VocabularyStageCountDto> StageBreakdown)
{
    public static VocabularySummaryDto FromDomain(VocabularyStats stats) => new(
        stats.Total, stats.Learning, stats.Mastered, stats.Due, stats.AddedLast7Days,
        stats.AddedLast30Days, stats.TotalFailCount,
        Enum.GetValues<ReviewStage>().Select(stage =>
            new VocabularyStageCountDto(stage, stats.StageBreakdown.GetValueOrDefault(stage))).ToList());
}

public sealed record ProgressSnapshotDto(
    Guid LearnerId,
    DateOnly AsOfDate,
    CefrLevel? OverallLevel,
    LevelStatusDto? LevelStatus,
    IReadOnlyList<ProgressSkillSnapshotDto> Skills,
    IReadOnlyList<ProgressErrorSnapshotDto> ErrorsLast30Days,
    TopicProgressSummaryDto TopicProgress,
    StudyStatsDto StudyTime,
    VocabularySummaryDto Vocabulary,
    GamificationStatusDto Gamification,
    bool HasLearningProfile);

public sealed record ProgressSkillInsightDto(
    SkillType Skill, int Priority, string EvidenceCode, string ActionCode,
    double Score, double EightWeekDelta, int SampleCount, ProgressDataConfidence Confidence);

public sealed record ProgressErrorInsightDto(
    ErrorCategory Category, int Priority, string ActionCode, int CountLast30Days);

public sealed record ProgressInsightAnalysisDto(
    string OverallCode,
    IReadOnlyList<string> AchievementCodes,
    IReadOnlyList<ProgressSkillInsightDto> SkillsToStrengthen,
    IReadOnlyList<ProgressErrorInsightDto> RecurringErrors,
    string HabitCode,
    string NextActionCode,
    string TargetRoute,
    ProgressInsightSource Source,
    bool IsFallback);

public sealed record ProgressInsightDto(
    ProgressSnapshotDto Snapshot,
    ProgressInsightAnalysisDto Insight,
    DateTimeOffset GeneratedAt);
