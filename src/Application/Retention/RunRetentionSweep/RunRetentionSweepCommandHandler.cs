using Application.Learning.Ports;
using Application.Notifications.Ports;
using Application.Retention.Dtos;
using Application.Vocabulary.Ports;
using Domain.Learning;
using Domain.Notifications;
using Domain.Retention;
using MediatR;

namespace Application.Retention.RunRetentionSweep;

/// <summary>
/// The daily win-back logic (PROJECT-SPEC I.2). For each learner it computes the inactivity
/// stage from <see cref="LearnerProfile.LastActivityAt"/> and dispatches the stage's
/// templated message only on the transition into that stage (dedup via
/// <see cref="LearnerProfile.LastWinBackStage"/>), so messages stay non-intrusive. A
/// returning learner's win-back state is reset so a future lapse restarts the ladder.
/// </summary>
public sealed class RunRetentionSweepCommandHandler
    : IRequestHandler<RunRetentionSweepCommand, RetentionSweepResultDto>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly IVocabularyRepository _vocabulary;
    private readonly INotificationDispatcher _dispatcher;
    private readonly INotificationTemplateProvider _templates;
    private readonly TimeProvider _clock;

    public RunRetentionSweepCommandHandler(
        ILearnerProfileRepository profiles,
        IVocabularyRepository vocabulary,
        INotificationDispatcher dispatcher,
        INotificationTemplateProvider templates,
        TimeProvider clock)
    {
        _profiles = profiles;
        _vocabulary = vocabulary;
        _dispatcher = dispatcher;
        _templates = templates;
        _clock = clock;
    }

    public async Task<RetentionSweepResultDto> Handle(
        RunRetentionSweepCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var profiles = await _profiles.GetAllAsync(cancellationToken);

        var sent = 0;

        foreach (var profile in profiles)
        {
            var stage = WinBackStage.ForDaysInactive(profile.DaysSinceLastActivity(now));

            // Learner is active again: clear the ladder so a future lapse re-triggers it.
            if (stage == InactivityStage.Active)
            {
                if (profile.LastWinBackStage != InactivityStage.Active)
                {
                    profile.ResetWinBack();
                    await _profiles.SaveAsync(profile, cancellationToken);
                }
                continue;
            }

            // Only message on the transition into a new stage (never repeat the same stage).
            if (stage == profile.LastWinBackStage)
                continue;

            await DispatchWinBackAsync(profile, stage, now, cancellationToken);
            profile.RecordWinBack(stage, now);
            await _profiles.SaveAsync(profile, cancellationToken);
            sent++;
        }

        return new RetentionSweepResultDto(ScannedLearners: profiles.Count, WinBackMessagesSent: sent);
    }

    private async Task DispatchWinBackAsync(
        LearnerProfile profile, InactivityStage stage, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var code = WinBackStage.CodeFor(stage)!; // non-Active stages always have a code

        // The 7-day message highlights how many words the learner has banked, to remind
        // them of progress worth protecting (PROJECT-SPEC I.2 step 2).
        IReadOnlyDictionary<string, string>? args = null;
        if (stage == InactivityStage.Day7)
        {
            var words = await _vocabulary.GetByLearnerIdAsync(profile.LearnerId, cancellationToken);
            args = new Dictionary<string, string> { ["count"] = words.Count.ToString() };
        }

        var message = _templates.Get(code, args) ?? code;
        var notification = Notification.Create(profile.LearnerId, code, message, now);
        await _dispatcher.SendAsync(notification, cancellationToken);
    }
}
