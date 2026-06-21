using Domain.Subscription;
using MediatR;

namespace Application.Subscription.CheckFeatureAccess;

/// <summary>
/// Reports whether a learner may currently use a metered feature (PROJECT-SPEC H.1), so the
/// UI can show remaining quota / upgrade prompts before the action is attempted.
/// </summary>
public sealed record CheckFeatureAccessQuery(Guid LearnerId, PremiumFeature Feature)
    : IRequest<GateDecisionDto>;

public sealed record GateDecisionDto(
    bool IsAllowed,
    int Limit,
    int Used,
    int? Remaining,
    UsagePeriod Period)
{
    public static GateDecisionDto From(GateDecision decision) =>
        new(decision.IsAllowed, decision.Limit, decision.Used, decision.Remaining, decision.Period);
}
