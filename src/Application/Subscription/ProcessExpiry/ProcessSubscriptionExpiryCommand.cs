using MediatR;

namespace Application.Subscription.ProcessExpiry;

/// <summary>
/// Daily subscription maintenance (PROJECT-SPEC H.3), run by Hangfire: sends renewal
/// reminders for subscriptions ending soon and transitions lapsed ones to Expired with a
/// notification. Uzbek text comes only from templates (docs/development-guide.md rule 11).
/// </summary>
public sealed record ProcessSubscriptionExpiryCommand : IRequest<ProcessExpiryResultDto>;

public sealed record ProcessExpiryResultDto(int RemindedLearners, int ExpiredSubscriptions);
