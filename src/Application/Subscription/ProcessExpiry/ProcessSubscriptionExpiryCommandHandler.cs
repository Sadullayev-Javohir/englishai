using Application.Notifications;
using Application.Notifications.Ports;
using Application.Subscription.Ports;
using Domain.Notifications;
using MediatR;

namespace Application.Subscription.ProcessExpiry;

public sealed class ProcessSubscriptionExpiryCommandHandler
    : IRequestHandler<ProcessSubscriptionExpiryCommand, ProcessExpiryResultDto>
{
    /// <summary>Remind when Premium ends within this many days (PROJECT-SPEC I.1 churn signal).</summary>
    public const int ReminderWindowDays = 3;

    private readonly ISubscriptionRepository _subscriptions;
    private readonly INotificationDispatcher _dispatcher;
    private readonly INotificationTemplateProvider _templates;
    private readonly TimeProvider _clock;

    public ProcessSubscriptionExpiryCommandHandler(
        ISubscriptionRepository subscriptions,
        INotificationDispatcher dispatcher,
        INotificationTemplateProvider templates,
        TimeProvider clock)
    {
        _subscriptions = subscriptions;
        _dispatcher = dispatcher;
        _templates = templates;
        _clock = clock;
    }

    public async Task<ProcessExpiryResultDto> Handle(
        ProcessSubscriptionExpiryCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var paid = await _subscriptions.GetPaidAsync(cancellationToken);

        var reminded = 0;
        var expired = 0;

        foreach (var subscription in paid)
        {
            if (subscription.ExpireIfElapsed(now))
            {
                await _subscriptions.SaveAsync(subscription, cancellationToken);
                await DispatchAsync(
                    subscription.LearnerId, NotificationCodes.SubscriptionExpired, now, null, cancellationToken);
                expired++;
                continue;
            }

            var daysLeft = subscription.DaysUntilExpiry(now);
            if (subscription.IsPremiumActive(now) && daysLeft is { } days && days <= ReminderWindowDays)
            {
                await DispatchAsync(
                    subscription.LearnerId,
                    NotificationCodes.SubscriptionExpiringSoon,
                    now,
                    new Dictionary<string, string> { ["days"] = days.ToString() },
                    cancellationToken);
                reminded++;
            }
        }

        return new ProcessExpiryResultDto(reminded, expired);
    }

    private async Task DispatchAsync(
        Guid learnerId,
        string code,
        DateTimeOffset now,
        IReadOnlyDictionary<string, string>? args,
        CancellationToken cancellationToken)
    {
        var message = _templates.Get(code, args) ?? code;
        var notification = Notification.Create(learnerId, code, message, now);
        await _dispatcher.SendAsync(notification, cancellationToken);
    }
}
