using Application.Learning.Ports;
using Application.Notifications.Ports;
using Application.Retention.RunRetentionSweep;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Notifications;
using Domain.Retention;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Retention;

/// <summary>
/// Unit tests for the daily win-back sweep (PROJECT-SPEC I.2): messages fire on stage
/// transitions, never repeat within a stage, and reset when the learner returns.
/// </summary>
public class RunRetentionSweepHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IVocabularyRepository _vocabulary = Substitute.For<IVocabularyRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly INotificationTemplateProvider _templates = Substitute.For<INotificationTemplateProvider>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    public RunRetentionSweepHandlerTests()
    {
        _templates.Get(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>())
            .Returns(ci => ci.ArgAt<string>(0));
        _vocabulary.GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<VocabularyItem>());
    }

    private static LearnerProfile ProfileInactiveFor(int days)
    {
        var createdAt = Now.AddDays(-days);
        return LearnerProfile.CreateFromPlacement(
            Guid.NewGuid(),
            new PlacementResult(CefrLevel.B1, CefrLevel.B1.ToScore(), new Dictionary<TestStage, StageResult>()),
            createdAt);
    }

    private RunRetentionSweepCommandHandler NewHandler() =>
        new(_profiles, _vocabulary, _dispatcher, _templates, _clock);

    [Fact]
    public async Task Sends_a_win_back_message_when_a_learner_enters_a_new_stage()
    {
        var profile = ProfileInactiveFor(7); // Day7 stage
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LearnerProfile> { profile });

        var result = await NewHandler().Handle(new RunRetentionSweepCommand(), CancellationToken.None);

        result.WinBackMessagesSent.Should().Be(1);
        result.ScannedLearners.Should().Be(1);
        profile.LastWinBackStage.Should().Be(InactivityStage.Day7);
        await _dispatcher.Received(1).SendAsync(
            Arg.Is<Notification>(n => n.Code == WinBackStage.Day7Code), Arg.Any<CancellationToken>());
        await _profiles.Received(1).SaveAsync(profile, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_repeat_a_message_for_the_same_stage()
    {
        var profile = ProfileInactiveFor(8); // still Day7 stage
        profile.RecordWinBack(InactivityStage.Day7, Now.AddDays(-1)); // already notified
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LearnerProfile> { profile });

        var result = await NewHandler().Handle(new RunRetentionSweepCommand(), CancellationToken.None);

        result.WinBackMessagesSent.Should().Be(0);
        await _dispatcher.DidNotReceive().SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resets_win_back_state_when_a_learner_becomes_active_again()
    {
        var profile = ProfileInactiveFor(0); // active today
        profile.RecordWinBack(InactivityStage.Day30, Now.AddDays(-5)); // was dormant before
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LearnerProfile> { profile });

        var result = await NewHandler().Handle(new RunRetentionSweepCommand(), CancellationToken.None);

        result.WinBackMessagesSent.Should().Be(0);
        profile.LastWinBackStage.Should().Be(InactivityStage.Active);
        await _profiles.Received(1).SaveAsync(profile, Arg.Any<CancellationToken>());
        await _dispatcher.DidNotReceive().SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_recently_active_learner_with_no_prior_win_back_is_left_alone()
    {
        var profile = ProfileInactiveFor(1); // Active stage, never messaged
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LearnerProfile> { profile });

        var result = await NewHandler().Handle(new RunRetentionSweepCommand(), CancellationToken.None);

        result.WinBackMessagesSent.Should().Be(0);
        await _profiles.DidNotReceive().SaveAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_seven_day_message_includes_the_learned_word_count()
    {
        var profile = ProfileInactiveFor(7);
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LearnerProfile> { profile });
        _vocabulary.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>())
            .Returns(new List<VocabularyItem>
            {
                VocabularyItem.Learn(profile.LearnerId, "apple", "olma", Now.AddDays(-20)),
                VocabularyItem.Learn(profile.LearnerId, "house", "uy", Now.AddDays(-20))
            });

        await NewHandler().Handle(new RunRetentionSweepCommand(), CancellationToken.None);

        _templates.Received().Get(
            WinBackStage.Day7Code,
            Arg.Is<IReadOnlyDictionary<string, string>?>(a => a != null && a["count"] == "2"));
    }
}
