using Application.Notifications.DispatchDailyActivityReminders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

public sealed class DailyPlanNotificationJob
{
    private readonly ISender _sender;
    private readonly ILogger<DailyPlanNotificationJob> _logger;

    public DailyPlanNotificationJob(ISender sender, ILogger<DailyPlanNotificationJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var notifiedLearners = await _sender.Send(new DispatchDailyActivityRemindersCommand());
        _logger.LogInformation(
            "Daily plan and streak-risk notifications sent to {Learners} learner(s).",
            notifiedLearners);
    }
}
