using MediatR;

namespace Application.Notifications.DispatchDailyActivityReminders;

/// <summary>
/// Sends a "finish your daily plan" native push to every onboarded learner who has not yet completed
/// today's plan (PROJECT-SPEC principle #4 - one gentle nudge per learner, never spam). The in-app
/// feed already derives these reminders live at read-time; this command is what delivers them to a
/// phone's status bar when the app is closed. Returns the number of learners pushed.
/// </summary>
public sealed record DispatchDailyActivityRemindersCommand : IRequest<int>;
