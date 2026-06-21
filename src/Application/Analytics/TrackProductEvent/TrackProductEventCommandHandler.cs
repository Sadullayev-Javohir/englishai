using Application.Analytics.Ports;
using Domain.Analytics;
using MediatR;

namespace Application.Analytics.TrackProductEvent;

public sealed class TrackProductEventCommandHandler : IRequestHandler<TrackProductEventCommand>
{
    private static readonly HashSet<ProductEventType> ClientTrackableEvents = new()
    {
        ProductEventType.SignedIn,
        ProductEventType.TopicOpened,
        ProductEventType.PronunciationFeedbackViewed,
        ProductEventType.PaywallHit,
        ProductEventType.UpgradeClicked,
    };

    private readonly IProductEventStore _events;
    private readonly TimeProvider _clock;

    public TrackProductEventCommandHandler(IProductEventStore events, TimeProvider clock)
    {
        _events = events;
        _clock = clock;
    }

    public async Task Handle(TrackProductEventCommand request, CancellationToken cancellationToken)
    {
        // Do not let the client forge authoritative milestones like Registered/Paid. Those are written
        // by server-side handlers only. UI-observed events (including a restored authenticated app-open
        // counted as SignedIn for D1 return) are idempotent by (learner, type, source).
        if (!ClientTrackableEvents.Contains(request.EventType))
            return;

        await _events.AppendOnceAsync(
            request.LearnerId, request.EventType, _clock.GetUtcNow(), request.Source, cancellationToken);
    }
}
