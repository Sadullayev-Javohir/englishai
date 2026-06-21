using System.Security.Cryptography;
using System.Text;
using Application.Common;
using Application.Gamification.Ports;
using Application.Notifications.Dtos;
using Application.Notifications.Ports;
using Application.Vocabulary.Ports;
using Domain.Learning;
using Domain.Vocabulary;
using MediatR;

namespace Application.Notifications.GetNotifications;

public sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, CursorPage<NotificationDto>>
{
    // The daily plan, ordered as the topic-centric learning path (learn words → grammar → writing
    // → speaking → listening → reading), each mapped to its reminder code and destination.
    private static readonly IReadOnlyList<(SkillType Skill, string Code, string LinkUrl)> DailySkills =
        new[]
        {
            (SkillType.Vocabulary, NotificationCodes.PracticeVocabulary, NotificationCodes.VocabularyLinkUrl),
            (SkillType.Grammar, NotificationCodes.PracticeGrammar, NotificationCodes.GrammarLinkUrl),
            (SkillType.Writing, NotificationCodes.PracticeWriting, NotificationCodes.WritingLinkUrl),
            (SkillType.Speaking, NotificationCodes.PracticeSpeaking, NotificationCodes.SpeakingLinkUrl),
            (SkillType.Listening, NotificationCodes.PracticeListening, NotificationCodes.ListeningLinkUrl),
            (SkillType.Reading, NotificationCodes.PracticeReading, NotificationCodes.ReadingLinkUrl),
        };

    // The daily "learn English" nudge appears from this local hour onward each day (08:00 Tashkent).
    private const int DailyLearnHour = 8;

    // Only broadcasts from the recent past are merged into the feed, so it can never grow unbounded.
    private static readonly TimeSpan BroadcastWindow = TimeSpan.FromDays(30);

    private readonly INotificationDispatcher _dispatcher;
    private readonly IVocabularyRepository _vocabulary;
    private readonly IVocabularyTopicRepository _topics;
    private readonly IGamificationStore _gamification;
    private readonly INotificationReadStateStore _readState;
    private readonly INotificationDismissalStore _dismissals;
    private readonly INotificationTemplateProvider _templates;
    private readonly IAdminBroadcastStore _broadcasts;
    private readonly INotificationArrivalStore _arrivals;
    private readonly TimeProvider _clock;

    public GetNotificationsQueryHandler(
        INotificationDispatcher dispatcher,
        IVocabularyRepository vocabulary,
        IVocabularyTopicRepository topics,
        IGamificationStore gamification,
        INotificationReadStateStore readState,
        INotificationDismissalStore dismissals,
        INotificationTemplateProvider templates,
        IAdminBroadcastStore broadcasts,
        INotificationArrivalStore arrivals,
        TimeProvider clock)
    {
        _dispatcher = dispatcher;
        _vocabulary = vocabulary;
        _topics = topics;
        _gamification = gamification;
        _readState = readState;
        _dismissals = dismissals;
        _templates = templates;
        _broadcasts = broadcasts;
        _arrivals = arrivals;
        _clock = clock;
    }

    public async Task<CursorPage<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var lastReadAt = await _readState.GetLastReadAtAsync(request.LearnerId, cancellationToken)
            ?? DateTimeOffset.MinValue;
        var dismissed = (await _dismissals.GetDismissedAsync(request.LearnerId, cancellationToken)).ToHashSet();

        var feed = new List<NotificationDto>();

        // The daily "learn English" reminder (kunlik eslatma), shown every day from 08:00 local.
        var dailyLearn = BuildDailyLearnReminder(request.LearnerId);
        if (dailyLearn is not null)
            feed.Add(dailyLearn);

        // Daily activity reminders (kunlik reja): derived from what the learner has practiced today.
        feed.AddRange(await BuildDailyRemindersAsync(request.LearnerId, cancellationToken));

        // SRS reminders are derived live from the learner's due words. All of a topic's due words
        // (any 3/7/21-day stage) collapse into a single per-topic reminder so the feed never shows one
        // row per word - the old behaviour that produced 15 notifications for 15 due words.
        feed.AddRange(await BuildReviewRemindersAsync(request.LearnerId, now, cancellationToken));

        // Dispatched notifications cover the non-SRS lifecycle messages (subscription, win-back).
        // The job-produced review_one/review_many reminders are superseded by the grouped reminders
        // above, so drop them here to avoid showing the same review twice.
        var dispatched = await _dispatcher.GetForLearnerAsync(request.LearnerId, cancellationToken);
        feed.AddRange(dispatched
            .Where(n => n.Code is not (NotificationCodes.ReviewOne or NotificationCodes.ReviewMany))
            .Select(NotificationDto.FromDomain));

        // Super-admin broadcasts appear in every learner's feed with their real send time; the shared
        // watermark below decides read-state just as it does for the derived reminders.
        var broadcasts = await _broadcasts.GetSinceAsync(now - BroadcastWindow, cancellationToken);
        feed.AddRange(broadcasts.Select(NotificationDto.FromBroadcast));

        // Read-state is the union of two mechanisms: the durable "mark all read" watermark (anything at
        // or before it is read) and the per-notification dismissal set (a single tapped notification).
        // Both make an item drop out of the client's unread feed.
        var cursor = StableCursor.Decode(request.Cursor);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var ordered = feed
            .Select(n => n with { IsRead = n.CreatedAt <= lastReadAt || dismissed.Contains(n.Id) })
            .Where(n => cursor is null
                || n.CreatedAt < cursor.Value.Timestamp
                || n.CreatedAt == cursor.Value.Timestamp && n.Id.CompareTo(cursor.Value.Id) < 0)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(pageSize + 1)
            .ToList();
        var items = ordered.Take(pageSize).ToList();
        return new CursorPage<NotificationDto>(items, ordered.Count > pageSize
            ? new StableCursor(items[^1].CreatedAt, items[^1].Id).Encode()
            : null);
    }

    // The daily "learn English" reminder: appears from 08:00 local each day, stamped with that 08:00 so
    // the feed shows a genuine arrival time. Deep-links to Home. Its id is day-scoped, so dismissing
    // today's occurrence hides only today's - tomorrow's reappears.
    private NotificationDto? BuildDailyLearnReminder(Guid learnerId)
    {
        var localNow = _clock.LocalNow();
        var eightLocal = new DateTimeOffset(
            localNow.Year, localNow.Month, localNow.Day, DailyLearnHour, 0, 0, AppClock.UzbekistanOffset);
        if (localNow < eightLocal)
            return null;

        var message = _templates.Get(NotificationCodes.DailyLearn);
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var id = DeterministicId(ArrivalKey(learnerId, NotificationCodes.DailyLearn, _clock.LocalToday()));
        return new NotificationDto(
            id, NotificationCodes.DailyLearn, message, eightLocal, IsRead: false, NotificationCodes.HomeLinkUrl);
    }

    private async Task<IReadOnlyList<NotificationDto>> BuildDailyRemindersAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        var today = _clock.LocalToday();
        var practicedToday = await _gamification.GetSkillsPracticedTodayAsync(learnerId, today, cancellationToken);

        // Which reminder codes apply right now: one gentle umbrella nudge when nothing is done yet
        // (never six at once - PROJECT-SPEC principle #4), otherwise a nudge for each skill still left
        // in today's plan (nothing once the plan is finished).
        var pending = practicedToday.Count == 0
            ? new[] { (Code: NotificationCodes.DailyPlan, LinkUrl: NotificationCodes.HomeLinkUrl) }
            : DailySkills
                .Where(s => !practicedToday.Contains(s.Skill))
                .Select(s => (s.Code, s.LinkUrl))
                .ToArray();

        if (pending.Length == 0)
            return Array.Empty<NotificationDto>();

        // Give every applicable reminder a genuine arrival time: the moment it was first observed for
        // this learner today, stamped durably per (learner, code, day) so it stays fixed across
        // refreshes and restarts. This replaces the old synthetic local-midnight timestamp (which read
        // as a misleading "00:00") while keeping the read watermark stable.
        var keysByCode = pending.ToDictionary(
            p => p.Code, p => ArrivalKey(learnerId, p.Code, today), StringComparer.Ordinal);
        var arrivals = await _arrivals.GetOrCreateAsync(
            keysByCode.Values.ToArray(), _clock.GetUtcNow(), cancellationToken);

        return pending
            .Select(p => BuildDaily(learnerId, p.Code, p.LinkUrl, keysByCode[p.Code], arrivals))
            .OfType<NotificationDto>()
            .ToList();
    }

    private NotificationDto? BuildDaily(
        Guid learnerId, string code, string linkUrl, string arrivalKey,
        IReadOnlyDictionary<string, DateTimeOffset> arrivals)
    {
        var message = _templates.Get(code);
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var createdAt = arrivals.TryGetValue(arrivalKey, out var seen) ? seen : _clock.GetUtcNow();

        // A stable id per (learner, code, day) so the client de-dupes across refreshes and the same
        // reminder never appears twice within a day.
        var id = DeterministicId(arrivalKey);
        return new NotificationDto(id, code, message, createdAt, IsRead: false, linkUrl);
    }

    // Stable identity of a reminder occurrence, shared by its arrival timestamp and its de-dupe id.
    private static string ArrivalKey(Guid learnerId, string code, DateOnly day) =>
        $"{learnerId:N}|{code}|{day:yyyy-MM-dd}";

    // Builds the SRS review reminders, grouped so each topic yields a single reminder ("«Topic» -
    // N words due") instead of one per word. Words with no source topic (manual/speaking/video
    // additions) collapse into one general reminder.
    private async Task<IReadOnlyList<NotificationDto>> BuildReviewRemindersAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var due = await _vocabulary.GetDueForLearnerAsync(learnerId, now, cancellationToken);
        if (due.Count == 0)
            return Array.Empty<NotificationDto>();

        var today = _clock.LocalToday();
        var result = new List<NotificationDto>();

        // Batch-resolve every distinct topic in one round-trip instead of one query per topic
        // (the old per-iteration await turned N distinct topics into N DB round-trips).
        var topicIds = due
            .Where(i => i.SourceTopicId is not null)
            .Select(i => i.SourceTopicId!.Value)
            .Distinct()
            .ToList();
        var topicsById = (await _topics.GetByIdsAsync(topicIds, cancellationToken))
            .ToDictionary(t => t.Id);

        foreach (var group in due.Where(i => i.SourceTopicId is not null)
                     .GroupBy(i => i.SourceTopicId!.Value))
        {
            var items = group.ToList();
            var topic = topicsById.TryGetValue(group.Key, out var found) ? found : null;

            // Resolve the per-topic template when the topic is known; a word whose topic no longer
            // exists falls back to the general count template so it is never dropped.
            var (code, message) = topic is not null
                ? (NotificationCodes.ReviewDueTopic, _templates.Get(
                    NotificationCodes.ReviewDueTopic,
                    new Dictionary<string, string> { ["topic"] = topic.TitleUz, ["count"] = items.Count.ToString() }))
                : (NotificationCodes.ReviewDueGeneral, _templates.Get(
                    NotificationCodes.ReviewDueGeneral,
                    new Dictionary<string, string> { ["count"] = items.Count.ToString() }));

            if (string.IsNullOrWhiteSpace(message))
                continue;

            // Day-scoped id so dismissing today's reminder hides only today's; the earliest checkpoint
            // among the topic's due words is its arrival time.
            var id = DeterministicId($"{learnerId:N}|review_topic|{group.Key:N}|{today:yyyy-MM-dd}");
            var createdAt = items.Min(i => i.Schedule.NextReviewAt ?? now);
            result.Add(new NotificationDto(
                id, code, message, createdAt, IsRead: false, NotificationCodes.ReviewLinkUrl));
        }

        var general = due.Where(i => i.SourceTopicId is null).ToList();
        if (general.Count > 0)
        {
            var message = _templates.Get(
                NotificationCodes.ReviewDueGeneral,
                new Dictionary<string, string> { ["count"] = general.Count.ToString() });

            if (!string.IsNullOrWhiteSpace(message))
            {
                var id = DeterministicId($"{learnerId:N}|review_general|{today:yyyy-MM-dd}");
                var createdAt = general.Min(i => i.Schedule.NextReviewAt ?? now);
                result.Add(new NotificationDto(
                    id, NotificationCodes.ReviewDueGeneral, message, createdAt, IsRead: false,
                    NotificationCodes.ReviewLinkUrl));
            }
        }

        return result;
    }

    // Derives a stable Guid from a string so recomputed (derived) notifications keep the same id
    // across fetches - the first 16 bytes of its SHA-256 digest, formatted as a Guid.
    private static Guid DeterministicId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16).ToArray());
    }
}
