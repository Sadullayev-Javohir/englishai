using Application.Analytics.Dtos;
using Application.Common;
using Application.Gamification.Dtos;
using Application.Learning.Dtos;
using MediatR;

namespace Application.Learning.GetPublicLearnerProgress;

public sealed record GetPublicLearnerProgressQuery(Guid LearnerId, DateOnly Today)
    : IRequest<PublicLearnerProgressDto>, IOwnershipExempt;

/// <summary>
/// Public, read-only learner progress projection for authenticated leaderboard viewers.
/// It intentionally excludes errors, AI insight, recommendations, coins and coupon data.
/// </summary>
public sealed record PublicLearnerProgressDto(
    Guid LearnerId,
    string DisplayName,
    string? PictureUrl,
    bool IsPremium,
    long LifetimeXp,
    int? Rank,
    Domain.Assessment.CefrLevel OverallLevel,
    IReadOnlyList<SkillScoreDto> SkillScores,
    int TopicsTotal,
    int TopicsLearned,
    int TopicsMastered,
    StudyStatsDto StudyStats,
    GamificationStatusDto Gamification,
    IReadOnlyList<GrowthPointDto> Growth);
