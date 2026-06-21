using Application.Gamification.Dtos;
using MediatR;

namespace Application.Gamification.GetPointsBalance;

/// <summary>
/// Reads a learner's points ledger, the fixed discount-tier catalog (annotated with what they
/// can currently afford), and their active (unused, unexpired) discount coupons.
/// </summary>
public sealed record GetPointsBalanceQuery(Guid LearnerId) : IRequest<PointsBalanceDto>;
