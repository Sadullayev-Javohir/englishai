using Application.Common;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Learning.GetLearnerOverview;

public sealed class GetLearnerOverviewQueryHandler
    : IRequestHandler<GetLearnerOverviewQuery, LearnerOverviewDto>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly TimeProvider _clock;

    public GetLearnerOverviewQueryHandler(ILearnerProfileRepository profiles, TimeProvider clock)
    {
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<LearnerOverviewDto> Handle(
        GetLearnerOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), request.LearnerId);

        return LearnerOverviewDto.FromDomain(profile, _clock.GetUtcNow());
    }
}
