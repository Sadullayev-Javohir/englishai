using Application.Subscription.Entitlements;
using MediatR;

namespace Application.Subscription.CheckFeatureAccess;

public sealed class CheckFeatureAccessQueryHandler : IRequestHandler<CheckFeatureAccessQuery, GateDecisionDto>
{
    private readonly IEntitlementService _entitlements;

    public CheckFeatureAccessQueryHandler(IEntitlementService entitlements)
    {
        _entitlements = entitlements;
    }

    public async Task<GateDecisionDto> Handle(CheckFeatureAccessQuery request, CancellationToken cancellationToken)
    {
        var decision = await _entitlements.EvaluateAsync(request.LearnerId, request.Feature, cancellationToken);
        return GateDecisionDto.From(decision);
    }
}
