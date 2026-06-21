using Application.Gamification;
using Application.Common;
using Application.Learning.Ports;
using Application.Reading.CheckReadingAnswer;
using Application.Reading.GetReadingCatalog;
using Application.Reading.GetReadingPassage;
using Application.Reading.Models;
using Application.Reading.Ports;
using Application.Reading.SubmitReadingQuiz;
using Application.Tests.Common;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Reading;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Reading;

public class ReadingHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly IReadingRepository _passages = Substitute.For<IReadingRepository>();
    private readonly IReadingContentGenerator _generator = Substitute.For<IReadingContentGenerator>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly IDailyProgressRecorder _dailyProgress = Substitute.For<IDailyProgressRecorder>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static VocabularyTopic Topic(CefrLevel level) =>
        VocabularyTopic.Curate(
            $"{level}-why-we-sleep".ToLowerInvariant(), "Why We Sleep", "Nega uxlaymiz",
            "science", "present-simple", level, Now);

    private static ReadingQuestion Q(int correct) =>
        ReadingQuestion.Create(
            "What is the key idea?", new[] { "A", "B", "C" }, correct, null, "Because the text says so.");

    private static ReadingPassage FilledPassage(Guid topicId, CefrLevel level, params ReadingQuestion[] questions)
    {
        var passage = ReadingPassage.ForTopic(topicId, "Why We Sleep", "science", level, Now);
        passage.FillContent(
            "Sleep is essential for the body and mind.",
            new[] { GlossaryEntry.Create("essential", "zarur") },
            questions.Length == 0 ? new[] { Q(0) } : questions);
        return passage;
    }

    private static GeneratedReadingContent Generated() =>
        new(
            "Sleep is essential for the body and mind.",
            new[] { new GeneratedReadingGlossary("essential", "zarur", "Sleep is essential.") },
            new[] { new GeneratedReadingQuestion("What is the key idea?", new[] { "A", "B", "C" }, 0, "Because A.") });

    private static LearnerProfile ProfileAt(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    [Fact]
    public async Task GetCatalog_returns_the_spine_topics_for_the_learners_level()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.B1) });
        var handler = new GetReadingCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(new GetReadingCatalogQuery(Learner), CancellationToken.None);

        catalog.Should().ContainSingle();
        catalog[0].Title.Should().Be("Why We Sleep");
        await _topics.Received(1).GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_defaults_to_A2_when_no_profile()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _topics.GetByLevelAsync(CefrLevel.A2, Arg.Any<CancellationToken>()).Returns(Array.Empty<VocabularyTopic>());
        var handler = new GetReadingCatalogQueryHandler(_topics, _profiles);

        await handler.Handle(new GetReadingCatalogQuery(Learner), CancellationToken.None);

        await _topics.Received(1).GetByLevelAsync(CefrLevel.A2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_browses_an_explicit_level_ignoring_the_learners_own_level()
    {
        // An explicit level pins exactly that band; the learner's profile must not be consulted.
        _topics.GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>())
            .Returns(new[] { Topic(CefrLevel.C1) });
        var handler = new GetReadingCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(
            new GetReadingCatalogQuery(Learner, CefrLevel.C1), CancellationToken.None);

        catalog.Should().ContainSingle();
        await _topics.Received(1).GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>());
        await _profiles.DidNotReceive().GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_all_levels_returns_the_whole_ladder_easiest_first()
    {
        foreach (var level in new[] { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 })
            _topics.GetByLevelAsync(level, Arg.Any<CancellationToken>()).Returns(new[] { Topic(level) });
        var handler = new GetReadingCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(
            new GetReadingCatalogQuery(Learner, AllLevels: true), CancellationToken.None);

        catalog.Should().HaveCount(6);
        await _topics.Received(1).GetByLevelAsync(CefrLevel.A1, Arg.Any<CancellationToken>());
        await _topics.Received(1).GetByLevelAsync(CefrLevel.C2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPassage_throws_when_topic_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);
        var handler = new GetReadingPassageQueryHandler(
            _topics, _passages, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var act = () => handler.Handle(new GetReadingPassageQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPassage_lazily_generates_caches_and_withholds_answers()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((ReadingPassage?)null);
        _generator.GenerateAsync(topic.Title, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(Generated());
        var handler = new GetReadingPassageQueryHandler(
            _topics, _passages, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetReadingPassageQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        dto.TopicId.Should().Be(topic.Id);
        dto.Questions.Should().ContainSingle();
        dto.Questions[0].GetType().GetProperty("CorrectOptionIndex").Should().BeNull();
        dto.Glossary.Should().ContainSingle();
        await _passages.Received(1).SaveAsync(
            Arg.Is<ReadingPassage>(p => p.VocabularyTopicId == topic.Id && p.IsFilled), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPassage_stays_pending_when_generation_unavailable()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((ReadingPassage?)null);
        _generator.GenerateAsync(topic.Title, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedReadingContent.Empty);
        var handler = new GetReadingPassageQueryHandler(
            _topics, _passages, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetReadingPassageQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeFalse();
        dto.Questions.Should().BeEmpty();
        await _passages.DidNotReceive().SaveAsync(Arg.Any<ReadingPassage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPassage_serves_the_cached_lesson_without_regenerating()
    {
        var topic = Topic(CefrLevel.B1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, Q(2));
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        var handler = new GetReadingPassageQueryHandler(
            _topics, _passages, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetReadingPassageQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        await _generator.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAnswer_returns_immediate_feedback_without_writing_progress()
    {
        var topic = Topic(CefrLevel.B1);
        var question = Q(1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, question);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        var handler = new CheckReadingAnswerQueryHandler(_topics, _passages);

        var correct = await handler.Handle(
            new CheckReadingAnswerQuery(topic.Id, question.Id, 1), CancellationToken.None);
        var wrong = await handler.Handle(
            new CheckReadingAnswerQuery(topic.Id, question.Id, 0), CancellationToken.None);

        correct.IsCorrect.Should().BeTrue();
        correct.Explanation.Should().BeNull();
        wrong.IsCorrect.Should().BeFalse();
        wrong.CorrectOptionIndex.Should().Be(1);
        wrong.Explanation.Should().Be("Because the text says so.");
        await _profiles.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        await _completions.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        await _dailyProgress.DidNotReceiveWithAnyArgs().RecordSkillAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task CheckAnswer_throws_when_question_is_unknown()
    {
        var topic = Topic(CefrLevel.B1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, Q(1));
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        var handler = new CheckReadingAnswerQueryHandler(_topics, _passages);

        var act = () => handler.Handle(
            new CheckReadingAnswerQuery(topic.Id, Guid.NewGuid(), 0), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CheckAnswer_rejects_an_option_outside_the_question()
    {
        var topic = Topic(CefrLevel.B1);
        var question = Q(1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, question);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        var handler = new CheckReadingAnswerQueryHandler(_topics, _passages);

        var act = () => handler.Handle(
            new CheckReadingAnswerQuery(topic.Id, question.Id, question.Options.Count), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task SubmitQuiz_grades_returns_explanation_records_skill_and_credits_topic()
    {
        var topic = Topic(CefrLevel.B1);
        var q1 = Q(1);
        var q2 = Q(2);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, q1, q2);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        var profile = ProfileAt(CefrLevel.B1);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((Domain.Vocabulary.TopicCompletionRecord?)null);
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitReadingQuizCommand(topic.Id, Learner, new[]
            {
                new ReadingQuizAnswer(q1.Id, 1), // correct
                new ReadingQuizAnswer(q2.Id, 0), // wrong
            }),
            CancellationToken.None);

        result.TotalQuestions.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        result.Outcomes.Single(o => o.QuestionId == q1.Id).Explanation.Should().BeNull();
        result.Outcomes.Single(o => o.QuestionId == q2.Id).Explanation.Should().Be("Because the text says so.");

        profile.Activities.Should().ContainSingle(a => a.Skill == SkillType.Reading);
        profile.Errors.Should().ContainSingle(error =>
            error.Skill == SkillType.Reading
            && error.Source == "reading_quiz"
            && error.SourceId == q2.Id
            && error.Explanation == "Because the text says so.");
        await _profiles.Received(1).TrackAsync(profile, Arg.Any<CancellationToken>());

        // The score is credited toward the topic's Reading module (K.5).
        result.Completion.Should().NotBeNull();
        await _completions.Received(1).TrackAsync(
            Arg.Is<Domain.Vocabulary.TopicCompletionRecord>(r => r.ScoreFor(SkillType.Reading) == 50),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_commits_the_profile_and_the_topic_record_together()
    {
        // The P1 failure boundary: two aggregates, one commit. Committing them separately let a
        // failure between them leave the learner's profile updated and their module score lost,
        // returning a 500 for work that had partly landed.
        var topic = Topic(CefrLevel.B1);
        var q1 = Q(1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, q1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        await handler.Handle(
            new SubmitReadingQuizCommand(topic.Id, Learner, new[] { new ReadingQuizAnswer(q1.Id, 1) }),
            CancellationToken.None);

        await _profiles.DidNotReceive().SaveAsync(
            Arg.Any<Domain.Learning.LearnerProfile>(), Arg.Any<CancellationToken>());
        await _completions.DidNotReceive().SaveAsync(
            Arg.Any<Domain.Vocabulary.TopicCompletionRecord>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_still_returns_the_result_when_the_daily_reward_fails()
    {
        // Streaks and XP live in Redis, the score in PostgreSQL. A Redis blip used to hand a learner
        // who had just answered correctly an HTTP 500 and a "try again" - re-submitting work that
        // was already committed. Losing a streak tick is the cheaper failure.
        var topic = Topic(CefrLevel.B1);
        var q1 = Q(1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, q1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        _dailyProgress.RecordSkillAsync(
                Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<Application.Gamification.Dtos.SkillRewardDto>(_ => throw new InvalidOperationException("redis down"));
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitReadingQuizCommand(topic.Id, Learner, new[] { new ReadingQuizAnswer(q1.Id, 1) }),
            CancellationToken.None);

        result.Passed.Should().BeTrue();
        result.Completion.Should().NotBeNull();
        await _completions.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_propagates_cancellation_rather_than_swallowing_it()
    {
        // A caller who left is not a failure to absorb: swallowing it would keep the request's work
        // running for a response nobody will read.
        var topic = Topic(CefrLevel.B1);
        var q1 = Q(1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, q1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        using var cancellation = new CancellationTokenSource();
        _dailyProgress.RecordSkillAsync(
                Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<Application.Gamification.Dtos.SkillRewardDto>(_ =>
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            });
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var act = () => handler.Handle(
            new SubmitReadingQuizCommand(topic.Id, Learner, new[] { new ReadingQuizAnswer(q1.Id, 1) }),
            cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SubmitQuiz_credits_topic_even_without_a_profile()
    {
        var topic = Topic(CefrLevel.B1);
        var q1 = Q(0);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, q1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((Domain.Vocabulary.TopicCompletionRecord?)null);
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitReadingQuizCommand(topic.Id, Learner, new[] { new ReadingQuizAnswer(q1.Id, 0) }),
            CancellationToken.None);

        result.Passed.Should().BeTrue();
        await _profiles.DidNotReceive().TrackAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).TrackAsync(Arg.Any<Domain.Vocabulary.TopicCompletionRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_below_pass_threshold_does_not_award_daily_progress()
    {
        var topic = Topic(CefrLevel.B1);
        var q1 = Q(1);
        var q2 = Q(1);
        var passage = FilledPassage(topic.Id, CefrLevel.B1, q1, q2);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(passage);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>())
            .Returns((Domain.Vocabulary.TopicCompletionRecord?)null);
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitReadingQuizCommand(topic.Id, Learner, new[]
            {
                new ReadingQuizAnswer(q1.Id, 0),
                new ReadingQuizAnswer(q2.Id, 0),
            }), CancellationToken.None);

        result.Passed.Should().BeFalse();
        await _dailyProgress.DidNotReceive().RecordSkillAsync(
            Learner, SkillType.Reading, Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_throws_when_lesson_not_generated_yet()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _passages.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((ReadingPassage?)null);
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var act = () => handler.Handle(
            new SubmitReadingQuizCommand(topic.Id, Learner, Array.Empty<ReadingQuizAnswer>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitQuiz_throws_when_topic_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);
        var handler = new SubmitReadingQuizCommandHandler(
            _topics, _passages, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var act = () => handler.Handle(
            new SubmitReadingQuizCommand(Guid.NewGuid(), Learner, Array.Empty<ReadingQuizAnswer>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
