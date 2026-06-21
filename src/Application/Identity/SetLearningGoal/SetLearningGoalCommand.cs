using Application.Identity.Dtos;
using Domain.Common;
using MediatR;

namespace Application.Identity.SetLearningGoal;

/// <summary>
/// Stores the learner's onboarding goal (goal-based onboarding). The user id comes from the
/// session (never the body), so a caller can only ever set their own goal. Returns the refreshed
/// authenticated user so the SPA can advance past the goal gate.
/// </summary>
public sealed record SetLearningGoalCommand(Guid UserId, LearningGoal Goal)
    : IRequest<AuthenticatedUserDto>;
