using Application.Analytics.Dtos;
using Application.Analytics.Ports;
using Domain.Analytics;
using MediatR;

namespace Application.Analytics.GetStudyStats;

public sealed class GetStudyStatsQueryHandler : IRequestHandler<GetStudyStatsQuery, StudyStatsDto>
{
    private readonly IStudyLogStore _store;

    public GetStudyStatsQueryHandler(IStudyLogStore store)
    {
        _store = store;
    }

    public async Task<StudyStatsDto> Handle(GetStudyStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _store.GetStatsAsync(request.LearnerId, request.Today, cancellationToken);
        return StudyStatsDto.FromDomain(stats);
    }
}
