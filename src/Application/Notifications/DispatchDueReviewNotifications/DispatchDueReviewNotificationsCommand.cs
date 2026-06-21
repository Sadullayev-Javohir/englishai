using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Notifications.DispatchDueReviewNotifications;

/// <summary>
/// Finds every vocabulary item due for review and sends each learner one reminder
/// (PROJECT-SPEC B.1). Invoked by the daily Hangfire recurring job. Grouping by learner
/// keeps reminders to at most one per learner per run (PROJECT-SPEC principle #4).
/// </summary>
public sealed record DispatchDueReviewNotificationsCommand : IRequest<DispatchResultDto>;
