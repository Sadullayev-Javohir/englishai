using Domain.Analytics;
using MediatR;

namespace Application.Analytics.TrackProductEvent;

/// <summary>
/// Records a client-observed product milestone for the signed-in learner. Server-side handlers record
/// authoritative business events; this command is only for UI-observed actions such as a feedback card
/// being viewed or the paywall being opened from a locked card.
/// </summary>
public sealed record TrackProductEventCommand(
    Guid LearnerId,
    ProductEventType EventType,
    string? Source = null) : IRequest;
