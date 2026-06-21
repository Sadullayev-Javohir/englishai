using Application.Subscription.Dtos;
using MediatR;

namespace Application.Subscription.CancelSubscription;

/// <summary>
/// Cancels auto-renew for a learner's active subscription (PROJECT-SPEC Qism H). Access is
/// kept until the paid period ends; the subscription then expires via the daily job.
/// </summary>
public sealed record CancelSubscriptionCommand(Guid LearnerId) : IRequest<SubscriptionDto>;
