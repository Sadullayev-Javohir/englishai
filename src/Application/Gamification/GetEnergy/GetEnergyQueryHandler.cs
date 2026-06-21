using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Domain.Gamification;
using MediatR;

namespace Application.Gamification.GetEnergy;

public sealed class GetEnergyQueryHandler : IRequestHandler<GetEnergyQuery, EnergyDto>
{
    private readonly ILearnerPointsRepository _points;
    private readonly TimeProvider _clock;

    public GetEnergyQueryHandler(ILearnerPointsRepository points, TimeProvider clock)
    {
        _points = points;
        _clock = clock;
    }

    public async Task<EnergyDto> Handle(GetEnergyQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var points = await _points.GetOrCreateAsync(request.LearnerId, now, cancellationToken);
        if (points.RefreshEnergy(now))
            await _points.SaveAsync(points, cancellationToken);

        return new EnergyDto(
            points.Energy,
            EnergyPolicy.MaximumEnergy,
            points.NextEnergyRefillAt,
            points.FullEnergyRefillAt,
            EnergyOutcome.None);
    }
}
