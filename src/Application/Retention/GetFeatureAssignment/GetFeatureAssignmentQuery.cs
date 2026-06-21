using Application.Retention.Dtos;
using MediatR;

namespace Application.Retention.GetFeatureAssignment;

/// <summary>
/// The variant a learner is assigned for one feature-flag experiment (PROJECT-SPEC I.3),
/// so the consuming feature can branch (e.g. daily-goal size). Returns <c>null</c> when the
/// flag key is not defined.
/// </summary>
public sealed record GetFeatureAssignmentQuery(Guid LearnerId, string Key) : IRequest<FeatureAssignmentDto?>;
