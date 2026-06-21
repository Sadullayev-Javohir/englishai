using Application.Analytics.Dtos;
using MediatR;

namespace Application.Analytics.GetStudyStats;

/// <summary>
/// The learner's study-time statistics for the progress dashboard, computed relative to their
/// local <paramref name="Today"/> (supplied by the client so the day/week/month/year boundaries
/// match the learner's own clock).
/// </summary>
public sealed record GetStudyStatsQuery(Guid LearnerId, DateOnly Today) : IRequest<StudyStatsDto>;
