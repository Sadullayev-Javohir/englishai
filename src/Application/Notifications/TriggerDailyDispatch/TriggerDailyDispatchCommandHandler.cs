using Application.Common;
using Application.Identity.Dtos;
using Application.Notifications.DispatchDailyActivityReminders;
using Application.Notifications.DispatchDueReviewNotifications;
using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Notifications.TriggerDailyDispatch;

/// <summary>
/// Authorizes the caller as a super-admin (403 otherwise), then runs the daily reminder dispatch on
/// demand - the operator "send now" fallback from the admin console. Sends BOTH streams the scheduled
/// jobs would: the SRS due-review reminders AND a daily-plan nudge to learners who have not finished
/// today's plan. Reporting only the SRS count (often zero when nothing is due) made the button look
/// broken, so the returned <see cref="DispatchResultDto.NotifiedLearners"/> now counts learners
/// reached across both streams.
/// </summary>
public sealed class TriggerDailyDispatchCommandHandler
    : IRequestHandler<TriggerDailyDispatchCommand, DispatchResultDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly ISender _sender;

    public TriggerDailyDispatchCommandHandler(IAdminAuthorization admin, ISender sender)
    {
        _admin = admin;
        _sender = sender;
    }

    public async Task<DispatchResultDto> Handle(
        TriggerDailyDispatchCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Only a super-admin can trigger the notification dispatch.");

        // SRS due-review reminders (in-app + push) - the original daily job.
        var srs = await _sender.Send(new DispatchDueReviewNotificationsCommand(), cancellationToken);

        // Daily-plan reminders (push) to learners who have not completed today's plan - so the button
        // does something visible even when no words are due for review.
        var activityLearners = await _sender.Send(
            new DispatchDailyActivityRemindersCommand(), cancellationToken);

        // Learners can overlap between the two streams; summing slightly over-counts, which is an
        // acceptable operator metric (it never under-reports the reach of the dispatch).
        return srs with { NotifiedLearners = srs.NotifiedLearners + activityLearners };
    }
}
