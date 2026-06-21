using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Domain.Gamification;
using MediatR;

namespace Application.Gamification.GetPointsBalance;

public sealed class GetPointsBalanceQueryHandler : IRequestHandler<GetPointsBalanceQuery, PointsBalanceDto>
{
    private readonly ILearnerPointsRepository _points;
    private readonly IDiscountRedemptionRepository _redemptions;
    private readonly TimeProvider _clock;

    public GetPointsBalanceQueryHandler(
        ILearnerPointsRepository points, IDiscountRedemptionRepository redemptions, TimeProvider clock)
    {
        _points = points;
        _redemptions = redemptions;
        _clock = clock;
    }

    public async Task<PointsBalanceDto> Handle(GetPointsBalanceQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var points = await _points.GetOrCreateAsync(request.LearnerId, now, cancellationToken);

        var tiers = DiscountCatalog.Tiers
            .Select(t => new DiscountTierDto(t.CoinsCost, t.DiscountPercent, points.SpendableCoins >= t.CoinsCost))
            .ToList();

        var active = await _redemptions.GetActiveForLearnerAsync(request.LearnerId, now, cancellationToken);
        var redemptionDtos = active
            .Select(r => new RedemptionDto(r.Code, r.DiscountPercent, r.ExpiresAt))
            .ToList();

        return new PointsBalanceDto(points.LifetimeXp, points.SpendableCoins, tiers, redemptionDtos);
    }
}
