using Application.Gamification.Dtos;
using MediatR;

namespace Application.Gamification.GetGamificationStatus;

/// <summary>
/// Reads a learner's current daily-goal progress and streak for the Home dashboard
/// (PROJECT-SPEC Faza 5), without recording any new activity.
/// </summary>
public sealed record GetGamificationStatusQuery(Guid LearnerId) : IRequest<GamificationStatusDto>;
