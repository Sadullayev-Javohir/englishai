using Application.Subscription.Dtos;
using MediatR;

namespace Application.Subscription.GetSubscription;

/// <summary>
/// Reads a learner's subscription (PROJECT-SPEC Qism H). Learners without a stored
/// subscription are reported as the default free tier.
/// </summary>
public sealed record GetSubscriptionQuery(Guid LearnerId) : IRequest<SubscriptionDto>;
