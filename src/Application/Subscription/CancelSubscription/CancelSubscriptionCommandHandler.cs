using Application.Common;
using Application.Subscription.Dtos;
using Application.Subscription.Ports;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.CancelSubscription;

public sealed class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, SubscriptionDto>
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly TimeProvider _clock;

    public CancelSubscriptionCommandHandler(ISubscriptionRepository subscriptions, TimeProvider clock)
    {
        _subscriptions = subscriptions;
        _clock = clock;
    }

    public async Task<SubscriptionDto> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();

        var subscription = await _subscriptions.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
                           ?? throw new NotFoundException("Subscription", request.LearnerId);

        subscription.Cancel(now);
        await _subscriptions.SaveAsync(subscription, cancellationToken);

        return SubscriptionDto.From(subscription, now);
    }
}
