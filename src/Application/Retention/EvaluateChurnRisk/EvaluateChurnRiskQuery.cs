using Application.Retention.Dtos;
using MediatR;

namespace Application.Retention.EvaluateChurnRisk;

/// <summary>
/// Scores a single learner's drop-off risk on demand (PROJECT-SPEC I.1) for retention
/// dashboards and support tooling. Assembles a snapshot from the learner profile,
/// gamification streak, vocabulary SRS state and subscription, then runs the pure
/// <see cref="Domain.Retention.ChurnEvaluator"/>.
/// </summary>
public sealed record EvaluateChurnRiskQuery(Guid LearnerId) : IRequest<ChurnAssessmentDto>;
