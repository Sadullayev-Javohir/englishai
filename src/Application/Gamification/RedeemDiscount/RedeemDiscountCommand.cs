using Application.Gamification.Dtos;
using MediatR;

namespace Application.Gamification.RedeemDiscount;

/// <summary>
/// Spends <paramref name="CoinsCost"/> of the learner's <see cref="Domain.Gamification
/// .LearnerPoints.SpendableCoins"/> on a one-time Pro-discount coupon at that tier
/// (leaderboard/points feature). <paramref name="CoinsCost"/> must match one of
/// <see cref="Domain.Gamification.DiscountCatalog.Tiers"/> exactly.
/// </summary>
public sealed record RedeemDiscountCommand(Guid LearnerId, int CoinsCost) : IRequest<RedemptionDto>;
