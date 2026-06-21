using Application.Common;
using Application.Subscription.Access;
using Application.Subscription.Dtos;
using Application.Subscription.Ports;
using MediatR;

namespace Application.Subscription.GetSubscription;

public sealed class GetSubscriptionQueryHandler : IRequestHandler<GetSubscriptionQuery, SubscriptionDto>
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IProAccessService _proAccess;
    private readonly TimeProvider _clock;

    public GetSubscriptionQueryHandler(
        ISubscriptionRepository subscriptions, IProAccessService proAccess, TimeProvider clock)
    {
        _subscriptions = subscriptions;
        _proAccess = proAccess;
        _clock = clock;
    }

    public async Task<SubscriptionDto> Handle(GetSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var access = await _proAccess.EvaluateAsync(request.LearnerId, cancellationToken);
        if (access.IsComplimentary)
            return SubscriptionDto.Complimentary(request.LearnerId);

        var now = _clock.GetUtcNow();
        var subscription = await _subscriptions.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
                           ?? Domain.Subscription.Subscription.CreateFree(request.LearnerId, now);

        if (!access.IsPaidPremium && access.IsTrialActive && access.TrialExpiresAt is { } trialExpiry)
            return SubscriptionDto.Trial(request.LearnerId, subscription.Status, trialExpiry, now);

        return SubscriptionDto.From(subscription, now);
    }
}
