using Application.Gamification;
using Application.Common;
using Application.Grammar.CheckGrammarExercise;
using Application.Grammar.GetGrammarCatalog;
using Application.Grammar.GetGrammarLesson;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Application.Grammar.SubmitGrammarExercises;
using Application.Learning.Ports;
using Application.Tests.Common;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Grammar;

public class GrammarHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly IGrammarRepository _lessons = Substitute.For<IGrammarRepository>();
    private readonly IGrammarContentGenerator _generator = Substitute.For<IGrammarContentGenerator>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly IDailyProgressRecorder _dailyProgress = Substitute.For<IDailyProgressRecorder>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    // "articles" maps to ErrorCategory.Articles, exercising the focus → heatmap-category mapping.
    private static VocabularyTopic Topic(CefrLevel level) =>
        VocabularyTopic.Curate(
            $"{level}-the-museum".ToLowerInvariant(), "The Museum", "Muzey",
            "culture", "articles", level, Now);

    private static GrammarExercise Ex(int correct) =>
        GrammarExercise.Create(
            GrammarExerciseType.FillInBlank, "I saw ___ painting.", new[] { "a", "an", "the" },
            correct, null, "Because 'the' points at a known painting.");

    private static GrammarLesson FilledLesson(Guid topicId, CefrLevel level, params GrammarExercise[] exercises)
    {
        var lesson = GrammarLesson.ForTopic(topicId, "The Museum", ErrorCategory.Articles, level, "articles", Now);
        lesson.FillContent(
            "At the museum we use articles a lot.",
            "Use 'the' for a specific museum and 'a' for any museum.",
            new[] { GrammarExample.Create("We visited the museum yesterday.") },
            new[] { GrammarCommonMistake.Create("Odatda 'a' o'rniga 'the' ishlatiladi.") },
            exercises.Length == 0 ? new[] { Ex(0) } : exercises,
            new[] { GrammarApplicationTask.Create(SkillType.Writing, "Write three sentences.") });
        return lesson;
    }

    private static GeneratedGrammarContent Generated() =>
        new(
            "At the museum we use articles a lot.",
            "Use 'the' for a specific museum and 'a' for any museum.",
            new[]
            {
                new GeneratedGrammarExample("We visited the museum yesterday.", "Biz kecha muzeyga bordik."),
            },
            new[] { "Odatda 'a' o'rniga 'the' ishlatiladi." },
            new[]
            {
                new GeneratedGrammarExercise(
                    GrammarExerciseType.Recognition, "Which is correct?", new[] { "a", "the" }, 1, "Because A."),
            },
            new[] { new GeneratedGrammarApplicationTask(SkillType.Writing, "Write three sentences.") });

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
        var handler = new GetGrammarCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(new GetGrammarCatalogQuery(Learner), CancellationToken.None);

        catalog.Should().ContainSingle();
        catalog[0].Title.Should().Be("The Museum");
        catalog[0].GrammarFocusCode.Should().Be("articles");
        await _topics.Received(1).GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_defaults_to_A2_when_no_profile()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _topics.GetByLevelAsync(CefrLevel.A2, Arg.Any<CancellationToken>()).Returns(Array.Empty<VocabularyTopic>());
        var handler = new GetGrammarCatalogQueryHandler(_topics, _profiles);

        await handler.Handle(new GetGrammarCatalogQuery(Learner), CancellationToken.None);

        await _topics.Received(1).GetByLevelAsync(CefrLevel.A2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLesson_throws_when_topic_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);
        var handler = new GetGrammarLessonQueryHandler(
            _topics, _lessons, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var act = () => handler.Handle(new GetGrammarLessonQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetLesson_lazily_generates_caches_and_withholds_answers()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((GrammarLesson?)null);
        _generator.GenerateAsync(topic.Title, topic.GrammarFocusCode, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(Generated());
        var handler = new GetGrammarLessonQueryHandler(
            _topics, _lessons, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetGrammarLessonQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        dto.TopicId.Should().Be(topic.Id);
        dto.Explanation.Should().NotBeNullOrEmpty();
        dto.Exercises.Should().ContainSingle();
        dto.Exercises[0].GetType().GetProperty("CorrectOptionIndex").Should().BeNull();
        dto.ApplicationTasks.Should().ContainSingle();
        await _lessons.Received(1).SaveAsync(
            Arg.Is<GrammarLesson>(l => l.VocabularyTopicId == topic.Id && l.IsFilled), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLesson_stays_pending_when_generation_unavailable()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((GrammarLesson?)null);
        _generator.GenerateAsync(topic.Title, topic.GrammarFocusCode, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedGrammarContent.Empty);
        var handler = new GetGrammarLessonQueryHandler(
            _topics, _lessons, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetGrammarLessonQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeFalse();
        dto.Exercises.Should().BeEmpty();
        await _lessons.DidNotReceive().SaveAsync(Arg.Any<GrammarLesson>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLesson_serves_the_cached_lesson_without_regenerating()
    {
        var topic = Topic(CefrLevel.B1);
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, Ex(2));
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new GetGrammarLessonQueryHandler(
            _topics, _lessons, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetGrammarLessonQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        await _generator.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckExercise_returns_immediate_feedback_without_recording_progress()
    {
        var topic = Topic(CefrLevel.B1);
        var exercise = Ex(2);
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, exercise);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new CheckGrammarExerciseQueryHandler(_topics, _lessons);

        var correct = await handler.Handle(
            new CheckGrammarExerciseQuery(topic.Id, exercise.Id, 2), CancellationToken.None);
        var incorrect = await handler.Handle(
            new CheckGrammarExerciseQuery(topic.Id, exercise.Id, 0), CancellationToken.None);

        correct.IsCorrect.Should().BeTrue();
        correct.CorrectOptionIndex.Should().Be(2);
        correct.Explanation.Should().BeNull();
        incorrect.IsCorrect.Should().BeFalse();
        incorrect.CorrectOptionIndex.Should().Be(2);
        incorrect.Explanation.Should().Be("Because 'the' points at a known painting.");
        await _profiles.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        await _completions.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        await _dailyProgress.DidNotReceiveWithAnyArgs().RecordSkillAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task CheckExercise_throws_when_exercise_is_not_in_topic_lesson()
    {
        var topic = Topic(CefrLevel.B1);
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, Ex(1));
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new CheckGrammarExerciseQueryHandler(_topics, _lessons);

        var act = () => handler.Handle(
            new CheckGrammarExerciseQuery(topic.Id, Guid.NewGuid(), 0), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("the", true, 2)]
    [InlineData("unknown", false, -1)]
    public async Task Typed_check_uses_server_answer_even_if_the_client_supplies_a_correct_index(string text, bool correct, int selected)
    {
        var topic = Topic(CefrLevel.B1);
        var exercise = Ex(2);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(FilledLesson(topic.Id, CefrLevel.B1, exercise));
        var handler = new CheckGrammarExerciseQueryHandler(_topics, _lessons);
        var result = await handler.Handle(new CheckGrammarExerciseQuery(topic.Id, exercise.Id, 2, text), CancellationToken.None);
        result.IsCorrect.Should().Be(correct);
        result.SelectedOptionIndex.Should().Be(selected);
        await _completions.DidNotReceiveWithAnyArgs().CommitAsync(default);
    }

    [Fact]
    public async Task Typed_final_submission_regrades_text_instead_of_trusting_option_index()
    {
        var topic = Topic(CefrLevel.B1);
        var e1 = Ex(0);
        var e2 = Ex(2);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(FilledLesson(topic.Id, CefrLevel.B1, e1, e2));
        var handler = new SubmitGrammarExercisesCommandHandler(_topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);
        var result = await handler.Handle(new SubmitGrammarExercisesCommand(topic.Id, Learner, new[] {
            new GrammarExerciseAnswer(e1.Id, -1, " A "),
            new GrammarExerciseAnswer(e2.Id, 2, "unknown")
        }), CancellationToken.None);
        result.CorrectCount.Should().Be(1);
        result.Outcomes.Single(item => item.ExerciseId == e2.Id).SelectedOptionIndex.Should().Be(-1);
        result.Completion!.Modules.Single(module => module.Module == "Grammar").Score.Should().Be(50);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("finished", true)]
    public void Typed_query_validation_rejects_empty_answers(string text, bool valid)
    {
        new CheckGrammarExerciseQueryValidator().Validate(new CheckGrammarExerciseQuery(Guid.NewGuid(), Guid.NewGuid(), -1, text))
            .IsValid.Should().Be(valid);
    }

    [Fact]
    public async Task SubmitExercises_grades_returns_explanation_records_skill_heatmap_and_credits_topic()
    {
        var topic = Topic(CefrLevel.B1);
        var e1 = Ex(1);
        var e2 = Ex(2);
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, e1, e2);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var profile = ProfileAt(CefrLevel.B1);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);
        var handler = new SubmitGrammarExercisesCommandHandler(
            _topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitGrammarExercisesCommand(topic.Id, Learner, new[]
            {
                new GrammarExerciseAnswer(e1.Id, 1), // correct
                new GrammarExerciseAnswer(e2.Id, 0), // wrong
            }),
            CancellationToken.None);

        result.TotalExercises.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        result.Outcomes.Single(o => o.ExerciseId == e1.Id).Explanation.Should().BeNull();
        result.Outcomes.Single(o => o.ExerciseId == e2.Id).Explanation.Should().Be("Because 'the' points at a known painting.");

        // Grammar skill activity recorded (G.4) and one Articles error recorded (C.7).
        profile.Activities.Should().ContainSingle(a => a.Skill == SkillType.Grammar);
        profile.ErrorHeatmap(Now).Should().ContainKey(ErrorCategory.Articles).WhoseValue.Should().Be(1);
        await _profiles.Received(1).TrackAsync(profile, Arg.Any<CancellationToken>());

        // The score is credited toward the topic's Grammar module (K.5).
        result.Completion.Should().NotBeNull();
        await _completions.Received(1).TrackAsync(
            Arg.Is<TopicCompletionRecord>(r => r.ScoreFor(SkillType.Grammar) == 50), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitExercises_survives_a_reward_failure_and_commits_once()
    {
        // The P1 failure boundary: the score is committed with the profile in ONE write, and the
        // Redis-backed streak/XP update that follows cannot turn a graded exercise set into a 500.
        var topic = Topic(CefrLevel.B1);
        var e1 = Ex(1);
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, e1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        _dailyProgress.RecordSkillAsync(
                Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<Application.Gamification.Dtos.SkillRewardDto>(_ => throw new InvalidOperationException("redis down"));
        var handler = new SubmitGrammarExercisesCommandHandler(
            _topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitGrammarExercisesCommand(topic.Id, Learner, new[] { new GrammarExerciseAnswer(e1.Id, 1) }),
            CancellationToken.None);

        result.CorrectCount.Should().Be(1);
        await _completions.DidNotReceive().SaveAsync(
            Arg.Any<TopicCompletionRecord>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitExercises_credits_topic_even_without_a_profile()
    {
        var topic = Topic(CefrLevel.B1);
        var e1 = Ex(0);
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, e1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);
        var handler = new SubmitGrammarExercisesCommandHandler(
            _topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitGrammarExercisesCommand(topic.Id, Learner, new[] { new GrammarExerciseAnswer(e1.Id, 0) }),
            CancellationToken.None);

        result.Passed.Should().BeTrue();
        result.Completion!.Modules.Single(m => m.Module == "Grammar").Score.Should().Be(100);
        await _profiles.DidNotReceive().TrackAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).TrackAsync(Arg.Any<TopicCompletionRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitExercises_marks_grammar_learned_with_seven_of_ten_correct()
    {
        var topic = Topic(CefrLevel.B1);
        var exercises = Enumerable.Range(0, 10).Select(_ => Ex(0)).ToArray();
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, exercises);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);
        var handler = new SubmitGrammarExercisesCommandHandler(
            _topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);
        var answers = exercises
            .Select((exercise, index) => new GrammarExerciseAnswer(exercise.Id, index < 7 ? 0 : 1))
            .ToArray();

        var result = await handler.Handle(
            new SubmitGrammarExercisesCommand(topic.Id, Learner, answers),
            CancellationToken.None);

        result.ScorePercent.Should().Be(70);
        result.Passed.Should().BeTrue();
        result.Completion!.Modules.Single(module => module.Module == "Grammar").Passed.Should().BeTrue();
        await _completions.Received(1).TrackAsync(
            Arg.Is<TopicCompletionRecord>(record =>
                record.ScoreFor(SkillType.Grammar) == 70 && record.IsModulePassed(SkillType.Grammar)),
            Arg.Any<CancellationToken>());
        await _dailyProgress.Received(1).RecordSkillAsync(
            Learner, SkillType.Grammar, 70, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitExercises_does_not_mark_grammar_learned_with_six_of_ten_correct()
    {
        var topic = Topic(CefrLevel.B1);
        var exercises = Enumerable.Range(0, 10).Select(_ => Ex(0)).ToArray();
        var lesson = FilledLesson(topic.Id, CefrLevel.B1, exercises);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);
        var handler = new SubmitGrammarExercisesCommandHandler(
            _topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);
        var answers = exercises
            .Select((exercise, index) => new GrammarExerciseAnswer(exercise.Id, index < 6 ? 0 : 1))
            .ToArray();

        var result = await handler.Handle(
            new SubmitGrammarExercisesCommand(topic.Id, Learner, answers),
            CancellationToken.None);

        result.ScorePercent.Should().Be(60);
        result.Passed.Should().BeFalse();
        result.Completion!.Modules.Single(module => module.Module == "Grammar").Passed.Should().BeFalse();
        await _dailyProgress.DidNotReceiveWithAnyArgs().RecordSkillAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task SubmitExercises_throws_when_lesson_not_generated_yet()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _lessons.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((GrammarLesson?)null);
        var handler = new SubmitGrammarExercisesCommandHandler(
            _topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var act = () => handler.Handle(
            new SubmitGrammarExercisesCommand(topic.Id, Learner, Array.Empty<GrammarExerciseAnswer>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitExercises_throws_when_topic_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);
        var handler = new SubmitGrammarExercisesCommandHandler(
            _topics, _lessons, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var act = () => handler.Handle(
            new SubmitGrammarExercisesCommand(Guid.NewGuid(), Learner, Array.Empty<GrammarExerciseAnswer>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
