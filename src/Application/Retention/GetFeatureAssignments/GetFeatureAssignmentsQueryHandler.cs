using Application.Retention.Dtos;
using Application.Retention.Ports;
using MediatR;

namespace Application.Retention.GetFeatureAssignments;

public sealed class GetFeatureAssignmentsQueryHandler
    : IRequestHandler<GetFeatureAssignmentsQuery, IReadOnlyList<FeatureAssignmentDto>>
{
    private readonly IFeatureFlagRepository _flags;

    public GetFeatureAssignmentsQueryHandler(IFeatureFlagRepository flags)
    {
        _flags = flags;
    }

    public async Task<IReadOnlyList<FeatureAssignmentDto>> Handle(
        GetFeatureAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var flags = await _flags.GetAllAsync(cancellationToken);
        return flags.Select(f => FeatureAssignmentDto.From(f, request.LearnerId)).ToList();
    }
}
