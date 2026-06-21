using Application.Common;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Learning.GetGrowth;

public sealed class GetGrowthQueryHandler
    : IRequestHandler<GetGrowthQuery, IReadOnlyList<GrowthPointDto>>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly TimeProvider _clock;

    public GetGrowthQueryHandler(ILearnerProfileRepository profiles, TimeProvider clock)
    {
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<IReadOnlyList<GrowthPointDto>> Handle(
        GetGrowthQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), request.LearnerId);

        return profile.GrowthHistory(_clock.GetUtcNow(), request.Weeks)
            .Select(GrowthPointDto.FromDomain)
            .ToList();
    }
}
