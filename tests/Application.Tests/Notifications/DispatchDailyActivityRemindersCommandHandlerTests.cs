using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Notifications;
using Application.Notifications.DispatchDailyActivityReminders;
using Application.Notifications.Ports;
using Application.Tests.Learning;
using Domain.Assessment;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

public class DispatchDailyActivityRemindersCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 9, 0, 0, TimeSpan.Zero);

    private static readonly SkillType[] FullPlan =
    {
        SkillType.Vocabulary, SkillType.Grammar, SkillType.Writing,
        SkillType.Speaking, SkillType.Listening, SkillType.Reading,
    };

    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IGamificationStore _gamification = Substitute.For<IGamificationStore>();
    private readonly INotificationTemplateProvider _templates = Substitute.For<INotificationTemplateProvider>();
    private readonly IPushNotifier _push = Substitute.For<IPushNotifier>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    public DispatchDailyActivityRemindersCommandHandlerTests()
    {
        _templates.Get(NotificationCodes.DailyPlanStreakRisk, Arg.Any<IReadOnlyDictionary<string, string>?>())
            .Returns("Bugungi rejangizni yakunlang!");
    }

    private DispatchDailyActivityRemindersCommandHandler Handler() =>
        new(_profiles, _gamification, _templates, _push, _clock);

    private void PracticedToday(Guid learner, params SkillType[] skills) =>
        _gamification.GetSkillsPracticedTodayAsync(learner, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(skills);

    [Fact]
    public async Task Pushes_only_to_learners_who_have_not_finished_the_plan()
    {
        var done = LearnerProfile.CreateAtLevel(Guid.NewGuid(), CefrLevel.A2, Now);
        var partial = LearnerProfile.CreateAtLevel(Guid.NewGuid(), CefrLevel.B1, Now);
        var untouched = LearnerProfile.CreateAtLevel(Guid.NewGuid(), CefrLevel.A1, Now);
        _profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { done, partial, untouched });

        PracticedToday(done.LearnerId, FullPlan);
        PracticedToday(partial.LearnerId, SkillType.Vocabulary, SkillType.Grammar);
        PracticedToday(untouched.LearnerId); // nothing done yet

        var notified = await Handler().Handle(new DispatchDailyActivityRemindersCommand(), CancellationToken.None);

        notified.Should().Be(2);
        await _push.Received(1).NotifyUsersAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids =>
                ids.Count == 2 && ids.Contains(partial.LearnerId) && ids.Contains(untouched.LearnerId)
                && !ids.Contains(done.LearnerId)),
            Arg.Is<PushMessage>(m => m.Body == "Bugungi rejangizni yakunlang!" && m.LinkUrl == NotificationCodes.HomeLinkUrl),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pushes_nothing_when_every_learner_finished_their_plan()
    {
        var a = LearnerProfile.CreateAtLevel(Guid.NewGuid(), CefrLevel.A2, Now);
        _profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { a });
        PracticedToday(a.LearnerId, FullPlan);

        var notified = await Handler().Handle(new DispatchDailyActivityRemindersCommand(), CancellationToken.None);

        notified.Should().Be(0);
        await _push.DidNotReceive().NotifyUsersAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<PushMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sends_nothing_when_the_template_is_missing()
    {
        _templates.Get(NotificationCodes.DailyPlanStreakRisk, Arg.Any<IReadOnlyDictionary<string, string>?>())
            .Returns((string?)null);
        var a = LearnerProfile.CreateAtLevel(Guid.NewGuid(), CefrLevel.A2, Now);
        _profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { a });
        PracticedToday(a.LearnerId); // plan not done, but no template to send

        var notified = await Handler().Handle(new DispatchDailyActivityRemindersCommand(), CancellationToken.None);

        notified.Should().Be(0);
        await _push.DidNotReceive().NotifyUsersAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<PushMessage>(), Arg.Any<CancellationToken>());
    }
}
