using Application.Notifications.DispatchDueReviewNotifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

/// <summary>
/// The Hangfire recurring-job entry point for the daily SRS reminder (PROJECT-SPEC B.1,
/// scheduled once per day). It is a thin adapter: all logic lives in the
/// <see cref="DispatchDueReviewNotificationsCommand"/> handler, which is unit-tested
/// independently. Hangfire resolves this from DI and invokes <see cref="RunAsync"/>.
/// </summary>
public sealed class DueReviewNotificationJob
{
    private readonly ISender _sender;
    private readonly ILogger<DueReviewNotificationJob> _logger;

    public DueReviewNotificationJob(ISender sender, ILogger<DueReviewNotificationJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var result = await _sender.Send(new DispatchDueReviewNotificationsCommand());
        _logger.LogInformation(
            "SRS due-review notifications sent: {Learners} learner(s), {Items} due item(s).",
            result.NotifiedLearners, result.TotalDueItems);
    }
}
