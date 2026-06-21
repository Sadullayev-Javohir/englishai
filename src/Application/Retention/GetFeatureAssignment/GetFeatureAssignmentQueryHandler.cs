using Application.Retention.Dtos;
using Application.Retention.Ports;
using MediatR;

namespace Application.Retention.GetFeatureAssignment;

public sealed class GetFeatureAssignmentQueryHandler
    : IRequestHandler<GetFeatureAssignmentQuery, FeatureAssignmentDto?>
{
    private readonly IFeatureFlagRepository _flags;

    public GetFeatureAssignmentQueryHandler(IFeatureFlagRepository flags)
    {
        _flags = flags;
    }

    public async Task<FeatureAssignmentDto?> Handle(
        GetFeatureAssignmentQuery request, CancellationToken cancellationToken)
    {
        var flag = await _flags.GetByKeyAsync(request.Key, cancellationToken);
        return flag is null ? null : FeatureAssignmentDto.From(flag, request.LearnerId);
    }
}
