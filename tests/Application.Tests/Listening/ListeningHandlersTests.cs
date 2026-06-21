using Application.Gamification;
using Application.Common;
using Application.Learning.Ports;
using Application.Listening.CheckListeningAnswer;
using Application.Listening.GetListeningAudio;
using Application.Listening.GetListeningCatalog;
using Application.Listening.GetListeningExercise;
using Application.Listening.Models;
using Application.Listening.Ports;
using Application.Listening.SubmitListeningQuiz;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Tests.Common;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Listening;
using Domain.Speaking;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Listening;

public class ListeningHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly IListeningRepository _exercises = Substitute.For<IListeningRepository>();
    private readonly IListeningContentGenerator _generator = Substitute.For<IListeningContentGenerator>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly IDailyProgressRecorder _dailyProgress = Substitute.For<IDailyProgressRecorder>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static VocabularyTopic Topic(CefrLevel level) =>
        VocabularyTopic.Curate(
            $"{level}-at-the-cafe".ToLowerInvariant(), "At the Café", "Kafeda",
            "everyday", "past-simple", level, Now);

    private static ListeningQuestion Q(int correct) =>
        ListeningQuestion.Create("What did the speaker say?", new[] { "A", "B", "C" }, correct, null, "Because A is in the clip.");

    private static ListeningExercise FilledExercise(Guid topicId, CefrLevel level, params ListeningQuestion[] questions)
    {
        var exercise = ListeningExercise.ForTopic(topicId, "At the Café", "everyday", level, Now);
        exercise.FillContent(
            "I would like a cup of tea, please.",
            questions.Length == 0 ? new[] { Q(0) } : questions);
        return exercise;
    }

    private static GeneratedListeningContent Generated() =>
        new(
            "I would like a cup of tea, please.",
            new[]
            {
                new GeneratedListeningQuestion(
                    "What did the speaker order?", new[] { "Tea", "Coffee" }, 0, "The speaker asks for tea."),
            });

    private static LearnerProfile ProfileAt(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    /// <summary>Minimal in-test audio cache (Application.Tests does not reference Infrastructure).</summary>
    private sealed class FakeAudioCache : IListeningAudioCache
    {
        private readonly Dictionary<Guid, ListeningAudioContent> _audio = new();

        public Task<ListeningAudioContent?> GetAsync(Guid exerciseId, CancellationToken cancellationToken) =>
            Task.FromResult(_audio.TryGetValue(exerciseId, out var cached) ? cached : null);

        public Task SetAsync(Guid exerciseId, byte[] audio, string contentType, CancellationToken cancellationToken)
        {
            _audio[exerciseId] = new ListeningAudioContent(audio, contentType, Now, SizeBytes: audio.LongLength);
            return Task.CompletedTask;
        }

        public Task<ListeningAudioMetadata?> GetMetadataAsync(Guid exerciseId, CancellationToken cancellationToken) =>
            Task.FromResult(_audio.TryGetValue(exerciseId, out var audio)
                ? new ListeningAudioMetadata(audio.SizeBytes ?? 0, Now, audio.ContentType)
                : null);
    }

    [Fact]
    public async Task GetCatalog_returns_the_spine_topics_for_the_learners_level()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>()).Returns(new[] { Topic(CefrLevel.B1) });
        var handler = new GetListeningCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(new GetListeningCatalogQuery(Learner), CancellationToken.None);

        catalog.Should().ContainSingle();
        catalog[0].Title.Should().Be("At the Café");
        await _topics.Received(1).GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_defaults_to_A2_when_no_profile()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _topics.GetByLevelAsync(CefrLevel.A2, Arg.Any<CancellationToken>()).Returns(Array.Empty<VocabularyTopic>());
        var handler = new GetListeningCatalogQueryHandler(_topics, _profiles);

        await handler.Handle(new GetListeningCatalogQuery(Learner), CancellationToken.None);

        await _topics.Received(1).GetByLevelAsync(CefrLevel.A2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_browses_an_explicit_level_without_a_profile()
    {
        _topics.GetByLevelAsync(CefrLevel.C2, Arg.Any<CancellationToken>()).Returns(new[] { Topic(CefrLevel.C2) });
        var handler = new GetListeningCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(
            new GetListeningCatalogQuery(Learner, CefrLevel.C2), CancellationToken.None);

        catalog.Should().ContainSingle();
        await _topics.Received(1).GetByLevelAsync(CefrLevel.C2, Arg.Any<CancellationToken>());
        await _profiles.DidNotReceive().GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCatalog_returns_the_whole_ladder_when_all_levels_requested()
    {
        foreach (var lvl in new[] { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 })
            _topics.GetByLevelAsync(lvl, Arg.Any<CancellationToken>()).Returns(new[] { Topic(lvl) });
        var handler = new GetListeningCatalogQueryHandler(_topics, _profiles);

        var catalog = await handler.Handle(
            new GetListeningCatalogQuery(Learner, AllLevels: true), CancellationToken.None);

        // Six levels × one topic each, easiest-first.
        catalog.Should().HaveCount(6);
        catalog.Select(c => c.Level).Should().ContainInOrder(
            CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2);
        await _profiles.DidNotReceive().GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetExercise_throws_when_topic_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);
        var handler = new GetListeningExerciseQueryHandler(
            _topics, _exercises, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var act = () => handler.Handle(new GetListeningExerciseQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetExercise_lazily_generates_caches_and_withholds_answers()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((ListeningExercise?)null);
        _generator.GenerateAsync(topic.Title, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(Generated());
        var handler = new GetListeningExerciseQueryHandler(
            _topics, _exercises, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetListeningExerciseQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        dto.TopicId.Should().Be(topic.Id);
        dto.Transcript.Should().NotBeNullOrEmpty();
        dto.Questions.Should().ContainSingle();
        dto.Questions[0].GetType().GetProperty("CorrectOptionIndex").Should().BeNull();
        await _exercises.Received(1).SaveAsync(
            Arg.Is<ListeningExercise>(e => e.VocabularyTopicId == topic.Id && e.IsFilled), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetExercise_stays_pending_when_generation_unavailable()
    {
        var topic = Topic(CefrLevel.B1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns((ListeningExercise?)null);
        _generator.GenerateAsync(topic.Title, topic.Level, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedListeningContent.Empty);
        var handler = new GetListeningExerciseQueryHandler(
            _topics, _exercises, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetListeningExerciseQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeFalse();
        dto.Questions.Should().BeEmpty();
        await _exercises.DidNotReceive().SaveAsync(Arg.Any<ListeningExercise>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetExercise_serves_the_cached_exercise_without_regenerating()
    {
        var topic = Topic(CefrLevel.B1);
        var exercise = FilledExercise(topic.Id, CefrLevel.B1, Q(1));
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(exercise);
        var handler = new GetListeningExerciseQueryHandler(
            _topics, _exercises, _generator, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var dto = await handler.Handle(new GetListeningExerciseQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        await _generator.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAnswer_returns_immediate_feedback_without_writing_progress()
    {
        var topic = Topic(CefrLevel.A1);
        var question = Q(1);
        var exercise = FilledExercise(topic.Id, CefrLevel.A1, question);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(exercise);
        var content = Substitute.For<IListeningContentProvider>();
        var handler = new CheckListeningAnswerQueryHandler(_topics, _exercises, content);

        var correct = await handler.Handle(
            new CheckListeningAnswerQuery(topic.Id, question.Id, 1), CancellationToken.None);
        var wrong = await handler.Handle(
            new CheckListeningAnswerQuery(topic.Id, question.Id, 0), CancellationToken.None);

        correct.IsCorrect.Should().BeTrue();
        correct.CorrectOptionIndex.Should().Be(1);
        correct.Hint.Should().BeNull();
        wrong.IsCorrect.Should().BeFalse();
        wrong.Hint.Should().Be("Because A is in the clip.");
        await _profiles.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        await _completions.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
        await _dailyProgress.DidNotReceiveWithAnyArgs()
            .RecordSkillAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task CheckAnswer_rejects_an_option_outside_the_question()
    {
        var topic = Topic(CefrLevel.A1);
        var question = Q(1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>())
            .Returns(FilledExercise(topic.Id, CefrLevel.A1, question));
        var handler = new CheckListeningAnswerQueryHandler(
            _topics, _exercises, Substitute.For<IListeningContentProvider>());

        var act = () => handler.Handle(
            new CheckListeningAnswerQuery(topic.Id, question.Id, 3), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task SubmitQuiz_grades_resolves_explanation_records_skill_and_credits_topic()
    {
        var topic = Topic(CefrLevel.A1);
        var q1 = Q(1);
        var q2 = Q(2);
        var exercise = FilledExercise(topic.Id, CefrLevel.A1, q1, q2);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(exercise);
        var profile = ProfileAt(CefrLevel.A1);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);
        var content = Substitute.For<IListeningContentProvider>();
        var handler = new SubmitListeningQuizCommandHandler(
            _topics, _exercises, content, _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitListeningQuizCommand(topic.Id, Learner, new[]
            {
                new ListeningQuizAnswer(q1.Id, 1), // correct
                new ListeningQuizAnswer(q2.Id, 0), // wrong
            }),
            CancellationToken.None);

        result.TotalQuestions.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        result.Outcomes.Single(o => o.QuestionId == q1.Id).Hint.Should().BeNull();
        // The generated English explanation is surfaced for the wrong answer (immersion path).
        result.Outcomes.Single(o => o.QuestionId == q2.Id).Hint.Should().Be("Because A is in the clip.");

        // The Listening skill activity is recorded against the learner profile (G.4).
        profile.Activities.Should().ContainSingle(a => a.Skill == SkillType.Listening);
        profile.Errors.Should().ContainSingle(error =>
            error.Skill == SkillType.Listening
            && error.Source == "listening_quiz"
            && error.SourceId == q2.Id
            && error.LearnerAnswer == "A"
            && error.ExpectedAnswer == "C");
        await _profiles.Received(1).TrackAsync(profile, Arg.Any<CancellationToken>());

        // The score is credited toward the topic's Listening module (K.5).
        result.Completion.Should().NotBeNull();
        await _completions.Received(1).TrackAsync(
            Arg.Is<TopicCompletionRecord>(r => r.ScoreFor(SkillType.Listening) == 50), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_survives_a_reward_failure_and_commits_once()
    {
        // The P1 failure boundary: the score is committed with the profile in ONE write, and the
        // Redis-backed streak/XP update that follows cannot turn a graded quiz into a 500.
        var topic = Topic(CefrLevel.A1);
        var q1 = Q(1);
        var exercise = FilledExercise(topic.Id, CefrLevel.A1, q1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(exercise);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.A1));
        _dailyProgress.RecordSkillAsync(
                Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<Application.Gamification.Dtos.SkillRewardDto>(_ => throw new InvalidOperationException("redis down"));
        var handler = new SubmitListeningQuizCommandHandler(
            _topics, _exercises, Substitute.For<IListeningContentProvider>(), _profiles, _completions,
            _dailyProgress, TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitListeningQuizCommand(topic.Id, Learner, new[] { new ListeningQuizAnswer(q1.Id, 1) }),
            CancellationToken.None);

        result.Passed.Should().BeTrue();
        await _completions.DidNotReceive().SaveAsync(
            Arg.Any<TopicCompletionRecord>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_credits_topic_even_without_a_profile()
    {
        var topic = Topic(CefrLevel.A1);
        var q1 = Q(0);
        var exercise = FilledExercise(topic.Id, CefrLevel.A1, q1);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(exercise);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);
        var handler = new SubmitListeningQuizCommandHandler(
            _topics, _exercises, Substitute.For<IListeningContentProvider>(), _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var result = await handler.Handle(
            new SubmitListeningQuizCommand(topic.Id, Learner, new[] { new ListeningQuizAnswer(q1.Id, 0) }),
            CancellationToken.None);

        result.Passed.Should().BeTrue();
        result.Completion!.Modules.Single(m => m.Module == "Listening").Score.Should().Be(100);
        await _profiles.DidNotReceive().TrackAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).TrackAsync(Arg.Any<TopicCompletionRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_records_daily_listening_only_after_a_passing_full_attempt()
    {
        var topic = Topic(CefrLevel.B2);
        var q1 = Q(0);
        var q2 = Q(1);
        var q3 = Q(2);
        var exercise = FilledExercise(topic.Id, CefrLevel.B2, q1, q2, q3);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(exercise);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _completions.GetAsync(Learner, topic.Id, Arg.Any<CancellationToken>()).Returns((TopicCompletionRecord?)null);
        var handler = new SubmitListeningQuizCommandHandler(
            _topics, _exercises, Substitute.For<IListeningContentProvider>(), _profiles, _completions, _dailyProgress,
            TopicAccessTestDoubles.AllowAll(), _clock);

        var incomplete = await handler.Handle(
            new SubmitListeningQuizCommand(topic.Id, Learner, new[] { new ListeningQuizAnswer(q3.Id, 2) }),
            CancellationToken.None);

        incomplete.Passed.Should().BeFalse();
        incomplete.TotalQuestions.Should().Be(3);
        await _dailyProgress.DidNotReceive().RecordSkillAsync(
            Arg.Any<Guid>(), Arg.Any<SkillType>(), Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());

        var passed = await handler.Handle(
            new SubmitListeningQuizCommand(topic.Id, Learner, new[]
            {
                new ListeningQuizAnswer(q1.Id, 0),
                new ListeningQuizAnswer(q2.Id, 1),
                new ListeningQuizAnswer(q3.Id, 2),
            }),
            CancellationToken.None);

        passed.Passed.Should().BeTrue();
        passed.CorrectCount.Should().Be(3);
        await _dailyProgress.Received(1).RecordSkillAsync(
            Learner, SkillType.Listening, 100, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAudio_synthesizes_once_then_serves_from_cache()
    {
        var topic = Topic(CefrLevel.A1);
        var exercise = FilledExercise(topic.Id, CefrLevel.A1);
        _exercises.GetByTopicIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(exercise);

        var wav = new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0 };
        var tts = Substitute.For<ITextToSpeechService>();
        var visemes = VisemeSequence.Create(new[] { new VisemeFrame(0, TimeSpan.Zero) }, TimeSpan.FromSeconds(1));
        tts.SynthesizeAsync(exercise.Transcript, Arg.Any<CancellationToken>())
            .Returns(new SynthesizedSpeech(wav, visemes));
        var cache = new FakeAudioCache();
        var handler = new GetListeningAudioQueryHandler(_exercises, tts, cache);

        var first = await handler.Handle(new GetListeningAudioQuery(topic.Id), CancellationToken.None);
        var second = await handler.Handle(new GetListeningAudioQuery(topic.Id), CancellationToken.None);

        first.Audio.Should().BeEquivalentTo(wav);
        first.ContentType.Should().Be("audio/wav");
        second.Audio.Should().BeEquivalentTo(wav);
        // Synthesized once; the second call is served from cache (rule 10).
        await tts.Received(1).SynthesizeAsync(exercise.Transcript, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAudio_throws_when_exercise_missing()
    {
        _exercises.GetByTopicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ListeningExercise?)null);
        var handler = new GetListeningAudioQueryHandler(
            _exercises, Substitute.For<ITextToSpeechService>(), new FakeAudioCache());

        var act = () => handler.Handle(new GetListeningAudioQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
