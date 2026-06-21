using Application.Analytics.Dtos;
using Application.Gamification.Dtos;
using Application.Learning.Dtos;
using Application.Retention.Dtos;

namespace Application.Admin.Dtos;

public sealed record AdminUserLearningDto(
    StudyStatsDto Study,
    GamificationStatusDto Gamification,
    IReadOnlyList<ProgressSkillSnapshotDto> Skills,
    IReadOnlyList<ProgressErrorSnapshotDto> Errors,
    TopicProgressSummaryDto Topics,
    VocabularySummaryDto Vocabulary,
    ChurnAssessmentDto Churn,
    int RegisteredDeviceCount,
    IReadOnlyList<string> DevicePlatforms);
