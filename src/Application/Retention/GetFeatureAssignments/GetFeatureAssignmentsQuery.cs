using Application.Retention.Dtos;
using MediatR;

namespace Application.Retention.GetFeatureAssignments;

/// <summary>
/// Every feature-flag experiment with the learner's assigned variant (PROJECT-SPEC I.3),
/// so the client can read all branches in one call.
/// </summary>
public sealed record GetFeatureAssignmentsQuery(Guid LearnerId) : IRequest<IReadOnlyList<FeatureAssignmentDto>>;
