using Application.Common;
using Application.Identity.Ports;
using Application.Notifications;
using Application.Notifications.Ports;
using Application.Subscription.Ports;
using Domain.Notifications;
using MediatR;

namespace Application.Identity.ProcessTrialExpiry;

public sealed class ProcessTrialExpiryCommandHandler
    : IRequestHandler<ProcessTrialExpiryCommand, ProcessTrialExpiryResult>
{
    /// <summary>
    /// Days-remaining marks that get a reminder. Two only: one to plan around and one that is the
    /// last chance. A daily countdown would train the learner to ignore the app's notifications.
    /// </summary>
    private static readonly int[] ReminderDays = [3, 1];

    private readonly IUserAccountStore _accounts;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly INotificationDispatcher _dispatcher;
    private readonly INotificationTemplateProvider _templates;
    private readonly IPaymentAvailability? _payments;
    private readonly TimeProvider _clock;

    public ProcessTrialExpiryCommandHandler(
        IUserAccountStore accounts,
        ISubscriptionRepository subscriptions,
        INotificationDispatcher dispatcher,
        INotificationTemplateProvider templates,
        TimeProvider clock,
        IPaymentAvailability? payments = null)
    {
        _accounts = accounts;
        _subscriptions = subscriptions;
        _dispatcher = dispatcher;
        _templates = templates;
        _clock = clock;
        _payments = payments;
    }

    public async Task<ProcessTrialExpiryResult> Handle(
        ProcessTrialExpiryCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var accounts = await _accounts.GetAllAsync(cancellationToken);
        // Someone who already pays does not need to hear about their trial.
        var paying = (await _subscriptions.GetPaidAsync(cancellationToken))
            .Where(subscription => subscription.IsPremiumActive(now))
            .Select(subscription => subscription.LearnerId)
            .ToHashSet();

        var reminded = 0;
        var expired = 0;

        foreach (var account in accounts)
        {
            if (paying.Contains(account.Id))
                continue;

            var localToday = now.ToLocalDate();
            var expiryDay = account.ProTrialExpiresAt.ToLocalDate();
            var daysLeft = expiryDay.DayNumber - localToday.DayNumber;

            // Compared on calendar days, not elapsed hours: "3 days left" has to mean the same thing
            // to the learner whether they registered at 09:00 or at 23:00. Running once a day, this
            // also means each mark fires exactly once - no de-duplication state needed.
            if (account.IsProTrialActive(now) && ReminderDays.Contains(daysLeft))
            {
                await DispatchAsync(
                    account.Id,
                    _payments?.PaymentsEnabled == true
                        ? NotificationCodes.TrialExpiringSoon
                        : NotificationCodes.TrialExpiringWaitlist,
                    new Dictionary<string, string> { ["days"] = daysLeft.ToString() },
                    now,
                    cancellationToken);
                reminded++;
                continue;
            }

            if (!account.IsProTrialActive(now) && daysLeft == 0)
            {
                await DispatchAsync(account.Id, NotificationCodes.TrialExpired, null, now, cancellationToken);
                expired++;
            }
        }

        return new ProcessTrialExpiryResult(reminded, expired);
    }

    private async Task DispatchAsync(
        Guid learnerId,
        string code,
        IReadOnlyDictionary<string, string>? args,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var message = _templates.Get(code, args) ?? code;
        await _dispatcher.SendAsync(Notification.Create(learnerId, code, message, now), cancellationToken);
    }
}
