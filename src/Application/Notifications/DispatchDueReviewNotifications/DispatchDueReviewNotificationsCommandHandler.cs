using Application.Notifications.Ports;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Notifications;
using MediatR;

namespace Application.Notifications.DispatchDueReviewNotifications;

/// <summary>
/// The daily SRS reminder logic (PROJECT-SPEC B.1). Loads all due words, groups them by
/// learner, and sends one templated reminder per learner - a single-word template when
/// only one word is due, a count template otherwise - so reminders never become spammy
/// (PROJECT-SPEC principle #4). Uzbek text comes only from templates (docs/development-guide.md rule 11).
/// </summary>
public sealed class DispatchDueReviewNotificationsCommandHandler
    : IRequestHandler<DispatchDueReviewNotificationsCommand, DispatchResultDto>
{
    // Native push for the SRS reminder deep-links straight to the review screen.
    private const string ReviewDeepLink = "/vocabulary/review";
    // Push status-bar title: the brand name (a proper noun, not translatable copy - rule 11 unaffected).
    private const string PushTitle = "EnglishAI.uz";

    private readonly IVocabularyRepository _vocabulary;
    private readonly INotificationDispatcher _dispatcher;
    private readonly INotificationTemplateProvider _templates;
    private readonly IPushNotifier _push;
    private readonly TimeProvider _clock;

    public DispatchDueReviewNotificationsCommandHandler(
        IVocabularyRepository vocabulary,
        INotificationDispatcher dispatcher,
        INotificationTemplateProvider templates,
        IPushNotifier push,
        TimeProvider clock)
    {
        _vocabulary = vocabulary;
        _dispatcher = dispatcher;
        _templates = templates;
        _push = push;
        _clock = clock;
    }

    public async Task<DispatchResultDto> Handle(
        DispatchDueReviewNotificationsCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var due = await _vocabulary.GetDueAsync(now, cancellationToken);

        var byLearner = due.GroupBy(item => item.LearnerId).ToList();

        foreach (var group in byLearner)
        {
            var items = group.ToList();
            var (code, message) = Build(items);
            var notification = Notification.Create(group.Key, code, message, now);
            await _dispatcher.SendAsync(notification, cancellationToken);

            // Also deliver as a native push so the reminder reaches the phone's status bar / app badge
            // when the app is closed. The body is the same vetted template text (rule 11); best-effort.
            await _push.NotifyUsersAsync(
                new[] { group.Key }, new PushMessage(PushTitle, message, ReviewDeepLink), cancellationToken);
        }

        return new DispatchResultDto(NotifiedLearners: byLearner.Count, TotalDueItems: due.Count);
    }

    private (string Code, string Message) Build(IReadOnlyList<Domain.Vocabulary.VocabularyItem> items)
    {
        if (items.Count == 1)
        {
            var code = NotificationCodes.ReviewOne;
            var args = new Dictionary<string, string> { ["word"] = items[0].Word };
            return (code, _templates.Get(code, args) ?? Fallback(code));
        }
        else
        {
            var code = NotificationCodes.ReviewMany;
            var args = new Dictionary<string, string> { ["count"] = items.Count.ToString() };
            return (code, _templates.Get(code, args) ?? Fallback(code));
        }
    }

    // The template store always ships these codes, but guard so a missing template never
    // produces an empty notification (Notification.Create rejects empty messages).
    private static string Fallback(string code) => code;
}
