using Application.Common;
using Application.Gamification;
using Application.Gamification.Ports;
using Application.Referral;
using Application.Tests.Common;
using Application.Tests.Learning;
using Application.Vocabulary.GetTopicCompletion;
using Application.Vocabulary.Ports;
using Application.Vocabulary.RecordTopicModuleScore;
using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Vocabulary;

public class TopicCompletionHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly IGamificationStore _gamification = Substitute.For<IGamificationStore>();
    private readonly ITopicVocabularyEnrollmentService _topicVocabularyEnrollment =
        Substitute.For<ITopicVocabularyEnrollmentService>();
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    public TopicCompletionHandlersTests()
    {
        // Default: no skills practiced yet today, so MarkDailyProgress runs its first-of-day path.
        _gamification.GetSkillsPracticedTodayAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SkillType>());
        _dailyProgress = new DailyProgressRecorder(
            _gamification, Substitute.For<IReferralService>(), Substitute.For<IPointsService>());
    }

    private static VocabularyTopic Topic()
    {
        var topic = VocabularyTopic.Curate(
            "b1-saving-water", "Saving Water", "Suvni tejash", "environment", "present-perfect", CefrLevel.B1, Now);
        topic.FillContent("Save water.", new[]
        {
            TopicWord.Create("reduce", "kamaytirmoq", "Reduce water waste."),
        });
        return topic;
    }

    [Fact]
    public async Task Record_keeps_the_module_score_when_the_reward_and_enrollment_fail()
    {
        // The P1 failure boundary: the score lives in PostgreSQL, the reward in Redis and the SRS
        // enrollment in another table. A failure in either of the latter two used to discard a module
        // score the learner had already earned; now they degrade to "no reward" instead.
        var topic = Topic();
        var learnerId = Guid.NewGuid();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _completions.GetAsync(Arg.Any<Guid>(), topic.Id, Arg.Any<CancellationToken>())
            .Returns((TopicCompletionRecord?)null);
        // A dedicated substitute rather than the shared recorder, so the failure cannot leak into
        // sibling tests.
        var failingRewards = Substitute.For<IDailyProgressRecorder>();
        failingRewards.RecordSkillAsync(
                Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<Application.Gamification.Dtos.SkillRewardDto>(_ => throw new InvalidOperationException("redis down"));
        var failingEnrollment = Substitute.For<ITopicVocabularyEnrollmentService>();
        failingEnrollment.EnrollAsync(
                Arg.Any<Guid>(), Arg.Any<VocabularyTopic>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<TopicVocabularyEnrollmentResult>(_ => throw new InvalidOperationException("enrollment down"));

        var handler = new RecordTopicModuleScoreCommandHandler(
            _topics, _completions, failingRewards, TopicAccessTestDoubles.AllowAll(),
            failingEnrollment, _clock);
        var result = await handler.Handle(
            new RecordTopicModuleScoreCommand(learnerId, topic.Id, SkillType.Vocabulary, 80),
            CancellationToken.None);

        result.Completion.Modules.Single(m => m.Module == "Vocabulary").Score.Should().Be(80);
        result.Reward.AwardedXp.Should().Be(0, "a reward that could not be applied is reported as none");
        await _completions.Received(1).SaveAsync(
            Arg.Is<TopicCompletionRecord>(r => r.ScoreFor(SkillType.Vocabulary) == 80),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Record_starts_a_new_record_and_records_the_module()
    {
        var topic = Topic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _completions.GetAsync(Arg.Any<Guid>(), topic.Id, Arg.Any<CancellationToken>())
            .Returns((TopicCompletionRecord?)null);
        var learnerId = Guid.NewGuid();

        var handler = new RecordTopicModuleScoreCommandHandler(
            _topics, _completions, _dailyProgress, TopicAccessTestDoubles.AllowAll(),
            _topicVocabularyEnrollment, _clock);
        var result = await handler.Handle(
            new RecordTopicModuleScoreCommand(learnerId, topic.Id, SkillType.Vocabulary, 80),
            CancellationToken.None);

        result.JustMastered.Should().BeFalse();
        result.Completion.Level.Should().Be("B1");
        result.Completion.PassedModuleCount.Should().Be(1);
        result.Completion.RequiredModuleCount.Should().Be(6);
        result.Completion.Modules.Single(m => m.Module == "Vocabulary").Passed.Should().BeTrue();
        await _completions.Received(1).SaveAsync(
            Arg.Is<TopicCompletionRecord>(r => r.LearnerId == learnerId && r.ScoreFor(SkillType.Vocabulary) == 80),
            Arg.Any<CancellationToken>());
        await _topicVocabularyEnrollment.Received(1).EnrollAsync(
            learnerId, topic, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Record_reports_mastery_when_the_final_module_passes()
    {
        var topic = Topic();
        var learnerId = Guid.NewGuid();
        var record = TopicCompletionRecord.Start(learnerId, topic.Id, topic.Level, Now);
        foreach (var module in TopicCompletionRecord.RequiredModules.Where(m => m != SkillType.Writing))
            record.RecordModule(module, 75, Now);

        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _completions.GetAsync(learnerId, topic.Id, Arg.Any<CancellationToken>()).Returns(record);

        var handler = new RecordTopicModuleScoreCommandHandler(
            _topics, _completions, _dailyProgress, TopicAccessTestDoubles.AllowAll(),
            _topicVocabularyEnrollment, _clock);
        var result = await handler.Handle(
            new RecordTopicModuleScoreCommand(learnerId, topic.Id, SkillType.Writing, 75),
            CancellationToken.None);

        result.JustMastered.Should().BeTrue();
        result.Completion.IsMastered.Should().BeTrue();
        result.Completion.PassedModuleCount.Should().Be(6);
        await _topicVocabularyEnrollment.DidNotReceive().EnrollAsync(
            Arg.Any<Guid>(), Arg.Any<VocabularyTopic>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Record_throws_when_topic_is_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);

        var handler = new RecordTopicModuleScoreCommandHandler(
            _topics, _completions, _dailyProgress, TopicAccessTestDoubles.AllowAll(),
            _topicVocabularyEnrollment, _clock);
        var act = () => handler.Handle(
            new RecordTopicModuleScoreCommand(Guid.NewGuid(), Guid.NewGuid(), SkillType.Reading, 90),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_returns_a_zeroed_checklist_when_not_started()
    {
        var topic = Topic();
        _completions.GetAsync(Arg.Any<Guid>(), topic.Id, Arg.Any<CancellationToken>())
            .Returns((TopicCompletionRecord?)null);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);

        var handler = new GetTopicCompletionQueryHandler(_completions, _topics, Substitute.For<IComplimentaryAccess>());
        var dto = await handler.Handle(
            new GetTopicCompletionQuery(Guid.NewGuid(), topic.Id), CancellationToken.None);

        dto.IsMastered.Should().BeFalse();
        dto.PassedModuleCount.Should().Be(0);
        dto.Modules.Should().HaveCount(6);
        dto.Modules.Should().OnlyContain(m => !m.Passed && m.Score == 0);
    }

    [Fact]
    public async Task Get_reflects_recorded_progress()
    {
        var topic = Topic();
        var learnerId = Guid.NewGuid();
        var record = TopicCompletionRecord.Start(learnerId, topic.Id, topic.Level, Now);
        record.RecordModule(SkillType.Vocabulary, 90, Now);
        record.RecordModule(SkillType.Reading, 50, Now);
        _completions.GetAsync(learnerId, topic.Id, Arg.Any<CancellationToken>()).Returns(record);

        var handler = new GetTopicCompletionQueryHandler(_completions, _topics, Substitute.For<IComplimentaryAccess>());
        var dto = await handler.Handle(
            new GetTopicCompletionQuery(learnerId, topic.Id), CancellationToken.None);

        dto.PassedModuleCount.Should().Be(1);
        dto.Modules.Single(m => m.Module == "Vocabulary").Passed.Should().BeTrue();
        dto.Modules.Single(m => m.Module == "Reading").Score.Should().Be(50);
        dto.Modules.Single(m => m.Module == "Reading").Passed.Should().BeFalse();
    }
}
