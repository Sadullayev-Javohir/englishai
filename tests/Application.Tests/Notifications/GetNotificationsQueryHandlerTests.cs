using Application.Gamification.Ports;
using Application.Notifications;
using Application.Notifications.GetNotifications;
using Application.Notifications.Ports;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Notifications;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

public class GetNotificationsQueryHandlerTests
{
    // 02:00 UTC = 07:00 Tashkent - before the 08:00 daily "learn English" reminder, so the existing
    // count-based tests exercise only the source they target. The dedicated daily-learn tests use their
    // own clock past 08:00.
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IVocabularyRepository _vocabulary = Substitute.For<IVocabularyRepository>();
    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly IGamificationStore _gamification = Substitute.For<IGamificationStore>();
    private readonly INotificationReadStateStore _readState = Substitute.For<INotificationReadStateStore>();
    private readonly INotificationDismissalStore _dismissals = Substitute.For<INotificationDismissalStore>();
    private readonly INotificationTemplateProvider _templates = new EchoTemplateProvider();
    private readonly IAdminBroadcastStore _broadcasts = Substitute.For<IAdminBroadcastStore>();
    private readonly INotificationArrivalStore _arrivals = Substitute.For<INotificationArrivalStore>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    public GetNotificationsQueryHandlerTests()
    {
        // Defaults: no due words, nothing dispatched, no broadcasts, never marked read. By default the
        // learner has practiced every skill today, so daily reminders are silent unless a test says
        // otherwise - keeping each test focused on the one feed source it exercises.
        _dispatcher.GetForLearnerAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Notification>());
        _vocabulary.GetDueForLearnerAsync(Learner, Now, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyItem>());
        _gamification.GetSkillsPracticedTodayAsync(Learner, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[] { SkillType.Vocabulary, SkillType.Grammar, SkillType.Writing, SkillType.Speaking, SkillType.Listening, SkillType.Reading });
        _readState.GetLastReadAtAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((DateTimeOffset?)null);
        _broadcasts.GetSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdminBroadcast>());
        // By default nothing is individually dismissed and topics resolve to null (words fall into the
        // general SRS bucket unless a test seeds a topic).
        _dismissals.GetDismissedAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((VocabularyTopic?)null);
        _topics.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyTopic>());
        // Model the first-seen store: each requested reminder key is stamped with the "now" it is first
        // observed (mirrors the real durable arrival - stable and a genuine time, not synthetic midnight).
        _arrivals.GetOrCreateAsync(
                Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var keys = ci.Arg<IReadOnlyCollection<string>>();
                var now = ci.Arg<DateTimeOffset>();
                return (IReadOnlyDictionary<string, DateTimeOffset>)keys
                    .ToDictionary(k => k, _ => now, StringComparer.Ordinal);
            });
    }

    private GetNotificationsQueryHandler Handler() =>
        new(_dispatcher, _vocabulary, _topics, _gamification, _readState, _dismissals, _templates,
            _broadcasts, _arrivals, _clock);

    // A word's stage climbs the ladder on each successful review: StartNew → Day3 → Day7 → Day21.
    private static VocabularyItem WordAtStage(string word, ReviewStage stage)
    {
        var item = VocabularyItem.Learn(Learner, word, "tarjima", Now.AddDays(-40));
        var passes = stage switch
        {
            ReviewStage.Day7 => 1,
            ReviewStage.Day21 => 2,
            _ => 0,
        };
        for (var i = 0; i < passes; i++)
            item.Schedule.RecordResult(passed: true, Now.AddDays(-40));
        return item;
    }

    // A due word that belongs to a vocabulary topic (so it groups under that topic's reminder).
    private static VocabularyItem WordInTopic(string word, Guid topicId) =>
        VocabularyItem.Learn(Learner, word, "tarjima", Now.AddDays(-40), sourceTopicId: topicId);

    [Fact]
    public async Task Groups_topicless_due_words_into_one_general_reminder()
    {
        // Three words due across different stages, none belonging to a topic → one grouped reminder,
        // not one per word (the old spammy behaviour).
        _vocabulary.GetDueForLearnerAsync(Learner, Now, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                WordAtStage("apple", ReviewStage.Day3),
                WordAtStage("bridge", ReviewStage.Day7),
                WordAtStage("courage", ReviewStage.Day21),
            });

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        var reviews = result.Items.Where(n => n.LinkUrl == NotificationCodes.ReviewLinkUrl).ToList();
        reviews.Should().ContainSingle();
        reviews[0].Code.Should().Be(NotificationCodes.ReviewDueGeneral);
        reviews[0].IsRead.Should().BeFalse();
        reviews[0].Message.Should().Contain("3"); // the count of due words
    }

    [Fact]
    public async Task Groups_due_words_by_topic_into_one_reminder_naming_the_topic()
    {
        // The topic's id is its own (VocabularyTopic.Curate assigns it internally) - the batch lookup
        // below keys results by that real id, so the due words must reference the same id a real
        // repository would return them under.
        var topic = VocabularyTopic.Curate(
            "a1-my-family", "My family", "Mening oilam", "daily life", "present-simple", CefrLevel.A1, Now);
        var topicId = topic.Id;
        _topics.GetByIdAsync(topicId, Arg.Any<CancellationToken>()).Returns(topic);
        _topics.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { topic });
        _vocabulary.GetDueForLearnerAsync(Learner, Now, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                WordInTopic("mother", topicId),
                WordInTopic("father", topicId),
            });

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        var reviews = result.Items.Where(n => n.LinkUrl == NotificationCodes.ReviewLinkUrl).ToList();
        reviews.Should().ContainSingle();
        reviews[0].Code.Should().Be(NotificationCodes.ReviewDueTopic);
        reviews[0].Message.Should().Contain("Mening oilam");
        reviews[0].Message.Should().Contain("2");
    }

    [Fact]
    public async Task Emits_daily_learn_reminder_from_eight_local_linking_to_home()
    {
        // 04:00 UTC = 09:00 Tashkent - past the 08:00 threshold, so the daily "learn English" nudge shows.
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 1, 4, 0, 0, TimeSpan.Zero));
        var handler = new GetNotificationsQueryHandler(
            _dispatcher, _vocabulary, _topics, _gamification, _readState, _dismissals, _templates,
            _broadcasts, _arrivals, clock);

        var result = await handler.Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        var learn = result.Items.Single(n => n.Code == NotificationCodes.DailyLearn);
        learn.LinkUrl.Should().Be(NotificationCodes.HomeLinkUrl);
        learn.IsRead.Should().BeFalse();
        // Its arrival time is 08:00 Tashkent (= 03:00 UTC), not the observing moment.
        learn.CreatedAt.Should().Be(new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.FromHours(5)));
    }

    [Fact]
    public async Task Hides_daily_learn_reminder_before_eight_local()
    {
        // The shared clock is 07:00 Tashkent - before 08:00, so the daily-learn nudge is absent.
        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        result.Items.Should().NotContain(n => n.Code == NotificationCodes.DailyLearn);
    }

    [Fact]
    public async Task Individually_dismissed_notification_comes_back_read()
    {
        _gamification.GetSkillsPracticedTodayAsync(Learner, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SkillType>());
        // Learn the daily-plan reminder's id, then mark exactly it dismissed.
        var first = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);
        var planId = first.Items.Single(n => n.Code == NotificationCodes.DailyPlan).Id;
        _dismissals.GetDismissedAsync(Learner, Arg.Any<CancellationToken>()).Returns(new[] { planId });

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        result.Items.Single(n => n.Code == NotificationCodes.DailyPlan).IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Keeps_lifecycle_notifications_but_drops_job_review_reminders()
    {
        var lifecycle = Notification.Create(Learner, NotificationCodes.SubscriptionExpired, "Premium tugadi.", Now);
        var jobReview = Notification.Create(Learner, NotificationCodes.ReviewMany, "Bugun 2 ta so'z.", Now);
        _dispatcher.GetForLearnerAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new[] { lifecycle, jobReview });

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Code.Should().Be(NotificationCodes.SubscriptionExpired);
    }

    [Fact]
    public async Task Emits_single_daily_plan_reminder_when_nothing_practiced_today()
    {
        _gamification.GetSkillsPracticedTodayAsync(Learner, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SkillType>());

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Code.Should().Be(NotificationCodes.DailyPlan);
        result.Items[0].LinkUrl.Should().Be(NotificationCodes.HomeLinkUrl);
        result.Items[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Emits_a_reminder_per_skill_still_left_in_todays_plan()
    {
        _gamification.GetSkillsPracticedTodayAsync(Learner, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[] { SkillType.Vocabulary, SkillType.Grammar });

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        result.Items.Select(n => n.Code).Should().BeEquivalentTo(new[]
        {
            NotificationCodes.PracticeWriting,
            NotificationCodes.PracticeSpeaking,
            NotificationCodes.PracticeListening,
            NotificationCodes.PracticeReading,
        });
        result.Items.Should().OnlyContain(n => !n.IsRead);
    }

    [Fact]
    public async Task Shows_no_daily_reminders_when_every_skill_is_done()
    {
        // The constructor default has all six skills practiced, so the daily section is empty.
        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Surfaces_admin_broadcast_with_its_title_and_real_send_time()
    {
        var sentAt = Now.AddMinutes(-15);
        var broadcast = AdminBroadcast.Create("Yangilik", "Yangi mavzular qo'shildi.", "/speaking", sentAt, Guid.NewGuid());
        _broadcasts.GetSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { broadcast });

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        var item = result.Items.Single(n => n.Code == NotificationCodes.AdminBroadcast);
        item.Title.Should().Be("Yangilik");
        item.Message.Should().Be("Yangi mavzular qo'shildi.");
        item.LinkUrl.Should().Be("/speaking");
        item.CreatedAt.Should().Be(sentAt);
        item.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Daily_reminder_carries_the_real_arrival_time_not_midnight()
    {
        _gamification.GetSkillsPracticedTodayAsync(Learner, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SkillType>());

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        // The arrival store stamps the reminder at the observing "now" (09:00), never local midnight.
        result.Items.Single().CreatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Read_watermark_marks_everything_at_or_before_it_as_read()
    {
        _gamification.GetSkillsPracticedTodayAsync(Learner, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SkillType>());
        // Marked read at "now" - the daily reminder's arrival time is stamped at that same "now", so it
        // is at-or-before the watermark and must come back read.
        _readState.GetLastReadAtAsync(Learner, Arg.Any<CancellationToken>()).Returns(Now);

        var result = await Handler().Handle(new GetNotificationsQuery(Learner), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].IsRead.Should().BeTrue();
    }

    private sealed class EchoTemplateProvider : INotificationTemplateProvider
    {
        public string? Get(string code, IReadOnlyDictionary<string, string>? args = null)
        {
            if (args is null)
                return code;

            var parts = new List<string> { code };
            if (args.TryGetValue("word", out var word)) parts.Add(word);
            if (args.TryGetValue("topic", out var topic)) parts.Add(topic);
            if (args.TryGetValue("count", out var count)) parts.Add(count);
            return string.Join(":", parts);
        }
    }
}
