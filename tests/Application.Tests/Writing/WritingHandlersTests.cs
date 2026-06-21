using Application.Ai;
using Application.Gamification;
using Application.Common;
using Application.Learning.Ports;
using Application.Subscription.Entitlements;
using Application.Tests.Common;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Application.Writing.GetWritingCatalog;
using Application.Writing.GetWritingTask;
using Application.Writing.Models;
using Application.Writing.Ports;
using Application.Writing.SubmitWriting;
using Domain.Assessment;
using Domain.Learning;
using Domain.Subscription;
using Domain.Vocabulary;
using Domain.Writing;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Writing;

public class WritingHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly IWritingTaskRepository _tasks = Substitute.For<IWritingTaskRepository>();
    private readonly IWritingPromptGenerator _generator = Substitute.For<IWritingPromptGenerator>();
    private readonly ITopicWritingAssessor _assessor = Substitute.For<ITopicWritingAssessor>();
    private readonly IWritingContentProvider _content = Substitute.For<IWritingContentProvider>();
    private readonly IEntitlementService _entitlements = Substitute.For<IEntitlementService>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly IDailyProgressRecorder _dailyProgress = Substitute.For<IDailyProgressRecorder>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static VocabularyTopic Topic(CefrLevel level = CefrLevel.A2) =>
        VocabularyTopic.Curate(
            "a2-busy-morning", "A Busy Morning", "Band ertalab", "daily-life", "past-simple", level, Now);

    private static WritingTask FilledTask(Guid topicId, CefrLevel level = CefrLevel.A2)
    {
        var task = WritingTask.ForTopic(topicId, level, 50, 80, Now);
        task.FillContent("Write a short message about your busy morning.", new[] { "Use simple sentences." });
        return task;
    }

    private static LearnerProfile ProfileAt(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    private static WritingAssessment Assessment(Guid taskId, params WritingIssue[] issues) =>
        WritingAssessment.Create(taskId, new[]
        {
            new DimensionScore(WritingDimension.TaskAchievement, 4),
            new DimensionScore(WritingDimension.Coherence, 3),
            new DimensionScore(WritingDimension.LexicalResource, 4),
            new DimensionScore(WritingDimension.GrammaticalAccuracy, 3),
        }, issues);

    private static WritingAssessment PassingAssessment(Guid taskId) =>
        WritingAssessment.Create(taskId, new[]
        {
            new DimensionScore(WritingDimension.TaskAchievement, 5),
            new DimensionScore(WritingDimension.Coherence, 5),
            new DimensionScore(WritingDimension.LexicalResource, 5),
            new DimensionScore(WritingDimension.GrammaticalAccuracy, 5),
        }, Array.Empty<WritingIssue>());

    private SubmitWritingCommandHandler SubmitHandler() =>
        new(_topics, _tasks, _assessor, _content, _entitlements, _profiles, _completions,
            _dailyProgress, TopicAccessTestDoubles.AllowAll(), _clock);

    [Fact]
    public async Task GetCatalog_returns_the_spine_topics_for_the_learners_level()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.B1) });
        var handler = new GetWritingCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(new GetWritingCatalogQuery(Learner), CancellationToken.None);

        catalog.Should().ContainSingle();
        catalog[0].TopicId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCatalog_browses_an_explicit_level_ignoring_the_learners_own_level()
    {
        // An explicit level pins exactly that band; the learner's profile must not be consulted.
        _topics.GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.C1) });
        var handler = new GetWritingCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(
            new GetWritingCatalogQuery(Learner, CefrLevel.C1), CancellationToken.None);

        catalog.Should().ContainSingle();
        await _topics.Received(1).GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>());
        await _profiles.DidNotReceive().GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_all_levels_returns_the_whole_ladder_easiest_first()
    {
        foreach (var level in new[] { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 })
            _topics.GetByLevelAsync(level, Arg.Any<CancellationToken>()).Returns(new[] { Topic(level) });
        var handler = new GetWritingCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(
            new GetWritingCatalogQuery(Learner, AllLevels: true), CancellationToken.None);

        catalog.Should().HaveCount(6);
        await _topics.Received(1).GetByLevelAsync(CefrLevel.A1, Arg.Any<CancellationToken>());
        await _topics.Received(1).GetByLevelAsync(CefrLevel.C2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTask_generates_and_caches_on_first_open()
    {
        var topic = Topic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((WritingTask?)null);
        _generator.GenerateAsync(topic.Title, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedWritingPrompt("Write about your busy morning.", new[] { "Keep it simple." }));
        var handler = new GetWritingTaskQueryHandler(
            _topics, _tasks, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetWritingTaskQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        dto.Prompt.Should().Be("Write about your busy morning.");
        dto.MinWords.Should().Be(50);
        dto.Guidance.Should().ContainSingle();
        await _tasks.Received(1).SaveAsync(Arg.Any<WritingTask>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTask_returns_retryable_error_when_generation_is_unavailable()
    {
        var topic = Topic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((WritingTask?)null);
        _generator.GenerateAsync(topic.Title, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedWritingPrompt.Empty);
        var handler = new GetWritingTaskQueryHandler(
            _topics, _tasks, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var act = () => handler.Handle(new GetWritingTaskQuery(topic.Id), CancellationToken.None);

        var error = await act.Should().ThrowAsync<AiAdmissionException>();
        error.Which.Code.Should().Be("provider_unavailable");
        error.Which.StatusCode.Should().Be(503);
        await _tasks.DidNotReceive().SaveAsync(Arg.Any<WritingTask>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTask_throws_when_the_topic_is_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);
        var handler = new GetWritingTaskQueryHandler(
            _topics, _tasks, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var act = () => handler.Handle(new GetWritingTaskQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Submit_assesses_records_usage_resolves_uzbek_and_feeds_skill_and_heatmap()
    {
        var topic = Topic();
        var task = FilledTask(topic.Id);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(task);
        var profile = ProfileAt(CefrLevel.B1);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        var grammarIssue = new WritingIssue(
            WritingDimension.GrammaticalAccuracy, "writing.issue.subject_verb_agreement", 10, 15,
            ErrorCategory.SubjectVerbAgreement);
        _assessor.AssessAsync(task, Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns(Assessment(task.Id, grammarIssue));
        _content.GetIssueExplanation("writing.issue.subject_verb_agreement").Returns("Ega va kesim moslashmagan.");
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);

        const string completeSubmission =
            "He go to school every day.\n\nThe final paragraph must also reach the AI assessor unchanged.";

        var result = await SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, completeSubmission), CancellationToken.None);

        result.DimensionScores.Should().HaveCount(4);
        result.AssessmentSource.Should().Be(WritingAssessmentSource.Hermes);
        result.Issues.Should().ContainSingle();
        result.Issues[0].Explanation.Should().Be("Ega va kesim moslashmagan.");
        result.OverallPercent.Should().BeInRange(0, 100);
        result.Completion.Should().NotBeNull();

        // Gating: allowed-check before assessment, usage recorded after success.
        await _entitlements.Received(1).EnsureAllowedAsync(Learner, PremiumFeature.WritingAssessment, Arg.Any<CancellationToken>());
        await _assessor.Received(1).AssessAsync(
            task, completeSubmission, CefrLevel.B1, Arg.Any<CancellationToken>());
        await _entitlements.Received(1).RecordUsageAsync(Learner, PremiumFeature.WritingAssessment, Arg.Any<CancellationToken>());

        // Writing skill activity (G.4) + grammar issue in the heatmap (C.7).
        profile.Activities.Should().ContainSingle(a => a.Skill == SkillType.Writing);
        profile.ErrorHeatmap(Now).Should().ContainKey(ErrorCategory.SubjectVerbAgreement);
        await _profiles.Received(1).TrackAsync(profile, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_credits_the_topics_writing_module_without_requiring_a_profile()
    {
        var topic = Topic();
        var task = FilledTask(topic.Id);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(task);
        // No profile - topic credit should not require a learner profile (matches reading/grammar).
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _assessor.AssessAsync(task, Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns(PassingAssessment(task.Id));
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);

        var result = await SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, "A complete, well-written answer."),
            CancellationToken.None);

        result.OverallPercent.Should().Be(100);
        result.EstimatedLevel.Should().Be(CefrLevel.A2);
        result.Completion!.Level.Should().Be("A2");
        result.Completion.Modules.Single(m => m.Module == "Writing").Score.Should().Be(100);
        result.Completion.Modules.Single(m => m.Module == "Writing").Passed.Should().BeTrue();
        await _completions.Received(1).TrackAsync(
            Arg.Is<TopicCompletionRecord>(r =>
                r.LearnerId == Learner && r.ScoreFor(SkillType.Writing) == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_survives_a_reward_failure_and_commits_once()
    {
        // The P1 failure boundary: an assessment the learner already paid a quota unit for must not
        // be lost to a Redis-backed streak/XP blip, and the profile and topic record commit together.
        var topic = Topic();
        var task = FilledTask(topic.Id);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(task);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _assessor.AssessAsync(task, Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns(PassingAssessment(task.Id));
        _dailyProgress.RecordSkillAsync(
                Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<Application.Gamification.Dtos.SkillRewardDto>(_ => throw new InvalidOperationException("redis down"));

        var result = await SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, "A complete, well-written answer."),
            CancellationToken.None);

        result.OverallPercent.Should().Be(100);
        await _completions.DidNotReceive().SaveAsync(
            Arg.Any<TopicCompletionRecord>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_propagates_ai_failure_without_recording_usage_or_progress()
    {
        var topic = Topic();
        var task = FilledTask(topic.Id);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(task);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _assessor.AssessAsync(task, Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns<WritingAssessment>(_ => throw new AiAdmissionException(
                "provider_unavailable", "AI provider is temporarily unavailable.", 5, 503));
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>())
            .Returns((TopicCompletionRecord?)null);
        var act = () => SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, "I write a complete answer about my day."),
            CancellationToken.None);

        var error = await act.Should().ThrowAsync<AiAdmissionException>();
        error.Which.Code.Should().Be("provider_unavailable");
        await _entitlements.DidNotReceive().RecordUsageAsync(
            Learner, PremiumFeature.WritingAssessment, Arg.Any<CancellationToken>());
        await _completions.DidNotReceive().TrackAsync(
            Arg.Any<TopicCompletionRecord>(), Arg.Any<CancellationToken>());
        await _dailyProgress.DidNotReceive().RecordSkillAsync(
            Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_does_not_assess_when_the_feature_is_gated()
    {
        var topic = Topic();
        var task = FilledTask(topic.Id);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(task);
        _entitlements
            .When(e => e.EnsureAllowedAsync(Learner, PremiumFeature.WritingAssessment, Arg.Any<CancellationToken>()))
            .Do(_ => throw new FeatureLimitExceededException(PremiumFeature.WritingAssessment, 3, UsagePeriod.Monthly));

        var act = () => SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, "Some text."), CancellationToken.None);

        await act.Should().ThrowAsync<FeatureLimitExceededException>();
        await _assessor.DidNotReceive().AssessAsync(
            Arg.Any<WritingTask>(), Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>());
        await _entitlements.DidNotReceive().RecordUsageAsync(Learner, PremiumFeature.WritingAssessment, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_rejects_a_submission_over_the_level_word_cap_before_assessing()
    {
        // Cost guard (rule 10): a text far longer than the level's range is refused before the paid
        // LLM assessment runs and before any quota is consumed.
        var topic = Topic();
        var task = FilledTask(topic.Id); // A2: max 80 words → hard cap 104
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(task);
        var tooLong = string.Join(' ', Enumerable.Repeat("word", 200));

        var act = () => SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, tooLong), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        await _assessor.DidNotReceive().AssessAsync(
            Arg.Any<WritingTask>(), Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>());
        await _entitlements.DidNotReceive().EnsureAllowedAsync(Learner, PremiumFeature.WritingAssessment, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_throws_when_the_topic_is_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);

        var act = () => SubmitHandler().Handle(
            new SubmitWritingCommand(Guid.NewGuid(), Learner, "Text."), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Submit_throws_when_the_task_is_not_generated_yet()
    {
        var topic = Topic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((WritingTask?)null);

        var act = () => SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, "Some text."), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _assessor.DidNotReceive().AssessAsync(
            Arg.Any<WritingTask>(), Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_grades_against_the_learners_level_instead_of_a_higher_topic_level()
    {
        var topic = Topic(CefrLevel.A2);
        var task = FilledTask(topic.Id, CefrLevel.A2);
        var profile = ProfileAt(CefrLevel.A1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _tasks.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(task);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        _assessor.AssessAsync(task, Arg.Any<string>(), CefrLevel.A1, Arg.Any<CancellationToken>())
            .Returns(PassingAssessment(task.Id));
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>())
            .Returns((TopicCompletionRecord?)null);

        var result = await SubmitHandler().Handle(
            new SubmitWritingCommand(topic.Id, Learner, "I write a clear A1 answer about my day."),
            CancellationToken.None);

        result.OverallBand.Should().Be(5);
        result.EstimatedLevel.Should().Be(CefrLevel.A1);
        await _assessor.Received(1).AssessAsync(
            task, Arg.Any<string>(), CefrLevel.A1, Arg.Any<CancellationToken>());
    }
}
