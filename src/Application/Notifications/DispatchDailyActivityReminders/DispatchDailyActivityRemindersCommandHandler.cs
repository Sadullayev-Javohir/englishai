using Application.Common;
using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Notifications.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Notifications.DispatchDailyActivityReminders;

/// <summary>
/// The daily-plan reminder logic. Loads every onboarded learner, checks which skills they have
/// practiced today (Tashkent local day), and pushes a single umbrella reminder to anyone who has not
/// finished the full plan yet. Only a native push is sent - the in-app feed already synthesizes the
/// same reminder live at read-time, so dispatching an in-app copy would duplicate it. Uzbek text comes
/// only from the vetted <c>notify.daily_plan</c> template (docs/development-guide.md rule 11).
/// </summary>
public sealed class DispatchDailyActivityRemindersCommandHandler
    : IRequestHandler<DispatchDailyActivityRemindersCommand, int>
{
    // The full daily plan (topic-centric path): a learner who has practiced all of these today is done.
    private static readonly IReadOnlyList<SkillType> DailySkills = new[]
    {
        SkillType.Vocabulary,
        SkillType.Grammar,
        SkillType.Writing,
        SkillType.Speaking,
        SkillType.Listening,
        SkillType.Reading,
    };

    // Push deep-links to the home dashboard, where the daily plan lives.
    private const string HomeDeepLink = NotificationCodes.HomeLinkUrl;
    // Push status-bar title: the brand name (a proper noun, not translatable copy - rule 11 unaffected).
    private const string PushTitle = "EnglishAI.uz";

    private readonly ILearnerProfileRepository _profiles;
    private readonly IGamificationStore _gamification;
    private readonly INotificationTemplateProvider _templates;
    private readonly IPushNotifier _push;
    private readonly TimeProvider _clock;

    public DispatchDailyActivityRemindersCommandHandler(
        ILearnerProfileRepository profiles,
        IGamificationStore gamification,
        INotificationTemplateProvider templates,
        IPushNotifier push,
        TimeProvider clock)
    {
        _profiles = profiles;
        _gamification = gamification;
        _templates = templates;
        _push = push;
        _clock = clock;
    }

    public async Task<int> Handle(
        DispatchDailyActivityRemindersCommand request, CancellationToken cancellationToken)
    {
        // The reminder body is a vetted template; if it is somehow missing, send nothing rather than
        // an empty push.
        var message = _templates.Get(NotificationCodes.DailyPlanStreakRisk);
        if (string.IsNullOrWhiteSpace(message))
            return 0;

        var today = _clock.LocalToday();
        var profiles = await _profiles.GetAllAsync(cancellationToken);

        var pending = new List<Guid>();
        foreach (var profile in profiles)
        {
            var practiced = await _gamification.GetSkillsPracticedTodayAsync(
                profile.LearnerId, today, cancellationToken);

            // Skip learners who have already finished the whole plan today; nudge everyone else once.
            if (DailySkills.All(practiced.Contains))
                continue;

            pending.Add(profile.LearnerId);
        }

        if (pending.Count == 0)
            return 0;

        // One best-effort push per learner (the notifier fans out to each learner's devices and
        // swallows failures, so a push problem never derails the operator action).
        await _push.NotifyUsersAsync(
            pending, new PushMessage(PushTitle, message, HomeDeepLink), cancellationToken);

        return pending.Count;
    }
}
