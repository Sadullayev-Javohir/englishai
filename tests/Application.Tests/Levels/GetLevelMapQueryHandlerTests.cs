using Application.Common;
using Application.Learning.Ports;
using Application.Levels.Dtos;
using Application.Levels.GetLevelMap;
using Application.Tests.Common;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Levels;

public class GetLevelMapQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ITopicSpeakingProgressStore _speaking = Substitute.For<ITopicSpeakingProgressStore>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static LearnerProfile Profile(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    private static VocabularyTopic Topic(CefrLevel level, string en) =>
        VocabularyTopic.Curate($"{level}-{en}".ToLowerInvariant(), en, en, "daily life", "present-simple", level, Now);

    private GetLevelMapQueryHandler Handler()
    {
        _completions.GetByLearnerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TopicCompletionRecord>());
        return new(_topics, _profiles, _speaking, _completions, Substitute.For<IComplimentaryAccess>(), TopicAccessTestDoubles.AllowAll(), _clock);
    }

    [Fact]
    public async Task Returns_can_do_topics_and_progress_for_the_current_level()
    {
        var learned = Topic(CefrLevel.B1, "Travel");
        var notLearned = Topic(CefrLevel.B1, "Work");
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { learned, notLearned });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new[] { learned.Id });

        var map = await new GetLevelMapQueryHandler(
                _topics,
                _profiles,
                _speaking,
                _completions,
                Substitute.For<IComplimentaryAccess>(),
                TopicAccessTestDoubles.AllowAll(),
                _clock)
            .Handle(new GetLevelMapQuery(Learner), CancellationToken.None);

        map.Level.Should().Be(CefrLevel.B1);
        map.IsCurrentLevel.Should().BeTrue();
        map.CanDo.Should().HaveCount(4); // four communicative skills (M.1)
        map.CanDo.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.StatementEn) && c.StatementCode.StartsWith("level.b1."));
        map.Topics.Should().HaveCount(2);
        map.TopicsTotal.Should().Be(2);
        map.TopicsLearned.Should().Be(1);
        map.ActiveTopicId.Should().Be(learned.Id);
        map.RecommendedNextModule.Should().Be("Vocabulary");
        map.SkillScores.Should().HaveCount(6);
        map.Readiness.Should().NotBeNull();
    }

    [Fact]
    public async Task Browsing_a_non_current_level_omits_readiness()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.A2));
        _topics.GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.C1, "Science") });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var map = await Handler().Handle(new GetLevelMapQuery(Learner, CefrLevel.C1), CancellationToken.None);

        map.Level.Should().Be(CefrLevel.C1);
        map.IsCurrentLevel.Should().BeFalse();
        map.CurrentLevel.Should().Be(CefrLevel.A2);
        map.IsLevelUnlocked.Should().BeFalse();
        map.HasFullAccess.Should().BeFalse();
        map.Topics.Should().OnlyContain(t => t.IsLocked && t.Modules.All(m => !m.Unlocked));
        map.Readiness.Should().BeNull();
        map.SkillScores.Should().HaveCount(6); // scores still come from the profile
    }

    [Fact]
    public async Task Placement_unlocks_every_lower_level_topic_and_module_for_review()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.A2, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.A2, "Family"), Topic(CefrLevel.A2, "Travel") });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var map = await Handler().Handle(
            new GetLevelMapQuery(Learner, CefrLevel.A2), CancellationToken.None);

        map.IsCurrentLevel.Should().BeFalse();
        map.IsLevelUnlocked.Should().BeTrue();
        map.Topics.Should().OnlyContain(topic =>
            !topic.IsLocked && topic.Modules.Count == 6 && topic.Modules.All(module => module.Unlocked));
    }

    [Fact]
    public async Task Complimentary_account_can_open_every_level_topic_and_module()
    {
        var complimentary = Substitute.For<IComplimentaryAccess>();
        complimentary.HasFullAccessAsync(Learner, Arg.Any<CancellationToken>()).Returns(true);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.A2));
        _topics.GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.C1, "Science"), Topic(CefrLevel.C1, "Research") });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var map = await new GetLevelMapQueryHandler(
                _topics, _profiles, _speaking, _completions, complimentary,
                TopicAccessTestDoubles.AllowAll(), _clock)
            .Handle(new GetLevelMapQuery(Learner, CefrLevel.C1), CancellationToken.None);

        map.CurrentLevel.Should().Be(CefrLevel.A2);
        map.IsCurrentLevel.Should().BeFalse();
        map.IsLevelUnlocked.Should().BeTrue();
        map.HasFullAccess.Should().BeTrue();
        map.Topics.Should().OnlyContain(t => !t.IsLocked && t.Modules.All(m => m.Unlocked));
    }

    [Theory]
    [InlineData(CefrLevel.B1, null, LevelExitState.Current)]   // the learner's current level - actionable
    [InlineData(CefrLevel.B1, CefrLevel.A2, LevelExitState.Completed)] // a level already passed
    [InlineData(CefrLevel.B1, CefrLevel.C1, LevelExitState.Locked)]    // a higher level not yet reached
    [InlineData(CefrLevel.C2, CefrLevel.C2, LevelExitState.MaxLevel)]  // C2 has no next level - no exit test
    public async Task Exit_test_node_reflects_the_level_relative_to_the_learner(
        CefrLevel currentLevel, CefrLevel? browse, LevelExitState expected)
    {
        var browsed = browse ?? currentLevel;
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(currentLevel));
        _topics.GetByLevelAsync(browsed, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(browsed, "Science") });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var map = await Handler().Handle(new GetLevelMapQuery(Learner, browse), CancellationToken.None);

        map.ExitState.Should().Be(expected);
    }

    [Fact]
    public async Task Falls_back_to_default_level_and_no_scores_without_a_profile()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _topics.GetByLevelAsync(CefrLevel.A1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.A1, "My Family") });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var map = await Handler().Handle(new GetLevelMapQuery(Learner), CancellationToken.None);

        map.Level.Should().Be(CefrLevel.A1);
        map.CurrentLevel.Should().Be(CefrLevel.A1);
        map.IsCurrentLevel.Should().BeTrue();
        map.SkillScores.Should().BeEmpty();
        map.Readiness.Should().BeNull();
    }

    [Fact]
    public async Task Surfaces_per_topic_module_progress_and_mastered_count()
    {
        var mastered = Topic(CefrLevel.B1, "Travel");
        var inProgress = Topic(CefrLevel.B1, "Work");
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { mastered, inProgress });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var masteredRecord = TopicCompletionRecord.Start(Learner, mastered.Id, CefrLevel.B1, Now);
        foreach (var module in TopicCompletionRecord.RequiredModules)
            masteredRecord.RecordModule(module, 90, Now);
        var partialRecord = TopicCompletionRecord.Start(Learner, inProgress.Id, CefrLevel.B1, Now);
        partialRecord.RecordModule(SkillType.Vocabulary, 90, Now);
        partialRecord.RecordModule(SkillType.Reading, 90, Now);

        _completions.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new[] { masteredRecord, partialRecord });

        var map = await new GetLevelMapQueryHandler(_topics, _profiles, _speaking, _completions, Substitute.For<IComplimentaryAccess>(), TopicAccessTestDoubles.AllowAll(), _clock)
            .Handle(new GetLevelMapQuery(Learner), CancellationToken.None);

        var masteredDto = map.Topics.Single(t => t.Id == mastered.Id);
        masteredDto.IsMastered.Should().BeTrue();
        masteredDto.PassedModuleCount.Should().Be(6);
        masteredDto.RequiredModuleCount.Should().Be(6);

        var partialDto = map.Topics.Single(t => t.Id == inProgress.Id);
        partialDto.IsMastered.Should().BeFalse();
        partialDto.PassedModuleCount.Should().Be(2);
        // The first topic is mastered, so the in-progress frontier topic is unlocked.
        masteredDto.IsLocked.Should().BeFalse();
        partialDto.IsLocked.Should().BeFalse();

        map.TopicsMastered.Should().Be(1);
        map.ActiveTopicId.Should().Be(inProgress.Id);
        map.RecommendedNextModule.Should().Be("Grammar");
    }

    [Fact]
    public async Task Keeps_three_unmastered_topics_open_and_locks_the_fourth()
    {
        var first = Topic(CefrLevel.B1, "Travel");
        var second = Topic(CefrLevel.B1, "Work");
        var third = Topic(CefrLevel.B1, "Hobbies");
        var fourth = Topic(CefrLevel.B1, "Health");
        var fifth = Topic(CefrLevel.B1, "Nature");
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { first, second, third, fourth, fifth });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        // The first topic is mastered, so the next three unmastered topics remain open.
        var firstRecord = TopicCompletionRecord.Start(Learner, first.Id, CefrLevel.B1, Now);
        foreach (var module in TopicCompletionRecord.RequiredModules)
            firstRecord.RecordModule(module, 90, Now);
        _completions.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new[] { firstRecord });

        var map = await new GetLevelMapQueryHandler(_topics, _profiles, _speaking, _completions, Substitute.For<IComplimentaryAccess>(), TopicAccessTestDoubles.AllowAll(), _clock)
            .Handle(new GetLevelMapQuery(Learner), CancellationToken.None);

        map.Topics.Single(t => t.Id == first.Id).IsLocked.Should().BeFalse();
        map.Topics.Single(t => t.Id == second.Id).IsLocked.Should().BeFalse();
        map.Topics.Single(t => t.Id == third.Id).IsLocked.Should().BeFalse();
        map.Topics.Single(t => t.Id == fourth.Id).IsLocked.Should().BeFalse();
        map.Topics.Single(t => t.Id == fifth.Id).IsLocked.Should().BeTrue();
        map.ActiveTopicId.Should().Be(second.Id);
        map.RecommendedNextModule.Should().Be("Vocabulary");
    }

    [Fact]
    public async Task Current_topic_keeps_every_skill_unlocked()
    {
        var topic = Topic(CefrLevel.B1, "Travel");
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>()).Returns(new[] { topic });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>());

        var completion = TopicCompletionRecord.Start(Learner, topic.Id, CefrLevel.B1, Now);
        completion.RecordModule(SkillType.Vocabulary, 75, Now);
        completion.RecordModule(SkillType.Grammar, 69, Now);
        var handler = Handler();
        _completions.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>()).Returns(new[] { completion });

        var map = await handler.Handle(new GetLevelMapQuery(Learner), CancellationToken.None);
        var modules = map.Topics.Single().Modules.ToDictionary(module => module.Module);

        modules["Vocabulary"].Passed.Should().BeTrue();
        modules["Grammar"].Unlocked.Should().BeTrue();
        modules["Reading"].Unlocked.Should().BeTrue();
        modules["Writing"].Unlocked.Should().BeTrue();
        modules.Values.Should().OnlyContain(module => module.Unlocked);
        modules.Values.Count(module => !module.Passed).Should().Be(5);
        map.ActiveTopicId.Should().Be(topic.Id);
        map.RecommendedNextModule.Should().Be("Grammar");
    }

    [Fact]
    public async Task Completed_level_has_no_active_topic_or_next_module()
    {
        var topic = Topic(CefrLevel.B1, "Travel");
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>()).Returns(new[] { topic });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>());

        var completion = TopicCompletionRecord.Start(Learner, topic.Id, CefrLevel.B1, Now);
        foreach (var module in TopicCompletionRecord.RequiredModules)
            completion.RecordModule(module, 75, Now);
        _completions.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>()).Returns(new[] { completion });

        var map = await new GetLevelMapQueryHandler(
                _topics,
                _profiles,
                _speaking,
                _completions,
                Substitute.For<IComplimentaryAccess>(),
                TopicAccessTestDoubles.AllowAll(),
                _clock)
            .Handle(new GetLevelMapQuery(Learner), CancellationToken.None);

        map.ActiveTopicId.Should().BeNull();
        map.RecommendedNextModule.Should().BeNull();
    }

    [Fact]
    public async Task Recommends_the_exit_test_once_the_skill_mastery_bar_is_met()
    {
        var profile = Profile(CefrLevel.B1);
        foreach (var skill in Enum.GetValues<SkillType>())
        {
            profile.RecordActivity(skill, 95, Now);
            profile.RecordActivity(skill, 95, Now.AddMinutes(1));
        }
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.B1, "Travel") });
        _speaking.GetLearnedTopicIdsAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var map = await Handler().Handle(new GetLevelMapQuery(Learner), CancellationToken.None);

        map.Readiness.Should().NotBeNull();
        map.Readiness!.ExitTestRecommended.Should().BeTrue();
        map.Readiness.MasteredSkillCount.Should().BeGreaterThanOrEqualTo(map.Readiness.RequiredMasteredSkills);
    }
}
