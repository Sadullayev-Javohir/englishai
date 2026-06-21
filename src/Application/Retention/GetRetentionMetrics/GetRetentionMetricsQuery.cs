using Application.Retention.Dtos;
using MediatR;

namespace Application.Retention.GetRetentionMetrics;

/// <summary>
/// Cohort D1/D7/D30 retention across all learners for the growth dashboard
/// (PROJECT-SPEC I.4). Built from each learner profile's registration date and active days.
/// </summary>
public sealed record GetRetentionMetricsQuery : IRequest<RetentionMetricsDto>;
