using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Domain.Gamification;
using MediatR;

namespace Application.Gamification.ConsumeEnergy;

/// <summary>
/// Books the energy cost of starting a Video or Speaking activity. Rejection is reported as a
/// normal 200 response carrying <see cref="EnergyOutcome.Insufficient"/> - the caller decides
/// whether to block navigation - so an empty bar never enters the paywall funnel.
/// </summary>
public sealed class ConsumeEnergyCommandHandler : IRequestHandler<ConsumeEnergyCommand, EnergyDto>
{
    private readonly ILearnerPointsRepository _points;
    private readonly TimeProvider _clock;

    public ConsumeEnergyCommandHandler(ILearnerPointsRepository points, TimeProvider clock)
    {
        _points = points;
        _clock = clock;
    }

    public async Task<EnergyDto> Handle(
        ConsumeEnergyCommand request,
        CancellationToken cancellationToken)
    {
        var balance = await _points.ConsumeEnergyAsync(
            request.LearnerId,
            request.Action,
            request.ReferenceId,
            _clock.GetUtcNow(),
            cancellationToken);

        return new EnergyDto(
            balance.Current,
            EnergyPolicy.MaximumEnergy,
            balance.NextRefillAt,
            balance.FullRefillAt,
            balance.Outcome);
    }
}
