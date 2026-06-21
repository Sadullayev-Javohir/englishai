using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Domain.Gamification;
using MediatR;

namespace Application.Gamification.RedeemDiscount;

public sealed class RedeemDiscountCommandHandler : IRequestHandler<RedeemDiscountCommand, RedemptionDto>
{
    /// <summary>Bails out rather than looping forever in the astronomically unlikely case of repeat collisions.</summary>
    private const int MaxCodeGenerationAttempts = 5;

    private readonly ILearnerPointsRepository _points;
    private readonly IDiscountRedemptionRepository _redemptions;
    private readonly TimeProvider _clock;

    public RedeemDiscountCommandHandler(
        ILearnerPointsRepository points, IDiscountRedemptionRepository redemptions, TimeProvider clock)
    {
        _points = points;
        _redemptions = redemptions;
        _clock = clock;
    }

    public async Task<RedemptionDto> Handle(RedeemDiscountCommand request, CancellationToken cancellationToken)
    {
        // Validated already, but a valid tier is required to construct the redemption regardless.
        var tier = DiscountCatalog.FindByCoinsCost(request.CoinsCost)
            ?? throw new NotFoundException("DiscountTier", request.CoinsCost);

        var now = _clock.GetUtcNow();
        var points = await _points.GetOrCreateAsync(request.LearnerId, now, cancellationToken);
        points.Redeem(tier.CoinsCost, now);

        var code = await GenerateUniqueCodeAsync(cancellationToken);
        var redemption = DiscountRedemption.Create(request.LearnerId, tier, code, now);

        await _points.SaveAsync(points, cancellationToken);
        await _redemptions.SaveAsync(redemption, cancellationToken);

        return new RedemptionDto(redemption.Code, redemption.DiscountPercent, redemption.ExpiresAt);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (await _redemptions.GetByCodeAsync(code, cancellationToken) is null)
                return code;
        }

        throw new InvalidOperationException("Could not generate a unique discount redemption code.");
    }
}
