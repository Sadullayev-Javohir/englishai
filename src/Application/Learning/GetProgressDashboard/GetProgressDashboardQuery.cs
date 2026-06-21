using Application.Analytics.Dtos;
using Application.Gamification.Dtos;
using Application.Learning.Dtos;
using Application.Levels.Dtos;
using MediatR;

namespace Application.Learning.GetProgressDashboard;

public sealed record GetProgressDashboardQuery(Guid LearnerId, DateOnly Today)
    : IRequest<ProgressDashboardDto>;

public sealed record ProgressDashboardDto(
    StudyStatsDto StudyStats,
    ProgressInsightDto ProgressInsight,
    LearnerOverviewDto Overview,
    IReadOnlyList<GrowthPointDto> Growth,
    IReadOnlyList<RecommendationDto> Recommendations,
    GamificationStatusDto Gamification,
    LevelMapDto LevelMap);
