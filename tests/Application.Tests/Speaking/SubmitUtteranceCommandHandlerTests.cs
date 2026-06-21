using Application.Analytics.Ports;
using Application.Gamification;
using Application.Gamification.Dtos;
using Application.Common;
using Application.Identity.Ports;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Speaking.SubmitUtterance;
using Application.Subscription.Entitlements;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Identity;
using Domain.Learning;
using Domain.Speaking;
using Domain.Subscription;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class SubmitUtteranceCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

    private readonly IConversationStore _conversations = Substitute.For<IConversationStore>();
    private readonly ISpeechToTextService _stt = Substitute.For<ISpeechToTextService>();
    private readonly IPronunciationAssessor _assessor = Substitute.For<IPronunciationAssessor>();
    private readonly IConversationTutor _tutor = Substitute.For<IConversationTutor>();
    private readonly ITextToSpeechService _tts = Substitute.For<ITextToSpeechService>();
    private readonly IFeedbackTemplateProvider _feedback = Substitute.For<IFeedbackTemplateProvider>();
    private readonly ITopicSpeakingProgressStore _topicProgress = Substitute.For<ITopicSpeakingProgressStore>();
    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly IDailyProgressRecorder _dailyProgress = Substitute.For<IDailyProgressRecorder>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();
    private readonly ISpeakingPracticeWordRepository _practiceWords = Substitute.For<ISpeakingPracticeWordRepository>();
    private readonly IPhonemeVisualLibrary _phonemeLibrary = Substitute.For<IPhonemeVisualLibrary>();
    private readonly ISpeakingCurriculumProvider _curriculum = Substitute.For<ISpeakingCurriculumProvider>();
    private readonly MutableTimeProvider _clock = new(Now);

    private SubmitUtteranceCommandHandler CreateHandler() =>
        new(_conversations, _stt, _assessor, _tutor, _tts, _feedback, _topicProgress, _topics, _completions, _dailyProgress, _accounts, _events, _clock, _practiceWords, _phonemeLibrary, curriculum: _curriculum);

    [Fact]
    public async Task Handle_keeps_an_explicit_free_talk_topic_instead_of_rematching_curriculum()
    {
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A1, topic: "my_family");
        var audio = new byte[] { 1, 2, 3 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Accepted("My mother wakes up early", 0.96));
        _assessor.AssessAsync(audio, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("What does your mother like?");
        _tts.SynthesizeAsync("What does your mother like?", Arg.Any<CancellationToken>()).Returns(Speech());

        await CreateHandler().Handle(new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        await _curriculum.DidNotReceive().MatchAsync(
            Arg.Any<CefrLevel>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        session.Topic.Should().Be("my_family");
        session.CurriculumContext.Should().BeNull();
    }

    [Fact]
    public async Task Process_records_unique_authentic_words_that_need_practice()
    {
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.B1);
        var existing = SpeakingPracticeWord.Create(
            learnerId, "world", 62, PronunciationErrorType.Mispronunciation, Now.AddDays(-1));
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((Domain.Identity.UserAccount?)null);
        _stt.TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("hello world hello", 0.96));
        _assessor.AssessAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            new PronunciationResult(58, 58, 70, 90, new[]
            {
                new WordPronunciation("hello", 55, PronunciationErrorType.Mispronunciation),
                new WordPronunciation("world", 64, PronunciationErrorType.Mispronunciation),
                new WordPronunciation("hello", 72, PronunciationErrorType.Mispronunciation),
            }, isAuthentic: true));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Keep going.");
        _tts.SynthesizeAsync("Keep going.", Arg.Any<CancellationToken>()).Returns(Speech());
        _practiceWords.GetByLearnerAndWordAsync(learnerId, "world", Arg.Any<CancellationToken>()).Returns(existing);
        _phonemeLibrary.GetWordPhonetics(Arg.Any<string>()).Returns(call =>
            new WordPhonetics(call.Arg<string>(), "test", Array.Empty<string>()));

        await CreateHandler().Handle(new SubmitUtteranceCommand(session.Id, new byte[] { 1 }), CancellationToken.None);

        await _practiceWords.Received(2).SaveAsync(Arg.Any<SpeakingPracticeWord>(), Arg.Any<CancellationToken>());
        existing.ErrorCount.Should().Be(2);
        existing.LastAccuracyScore.Should().Be(64);
    }

    [Fact]
    public async Task Process_skips_words_without_pronunciation_details()
    {
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.B1);
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _stt.TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Accepted("known unsupported", 0.96));
        _assessor.AssessAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            new PronunciationResult(58, 58, 70, 90, new[]
            {
                new WordPronunciation("known", 55, PronunciationErrorType.Mispronunciation),
                new WordPronunciation("unsupported", 45, PronunciationErrorType.Mispronunciation),
            }, isAuthentic: true));
        _phonemeLibrary.GetWordPhonetics("known")
            .Returns(new WordPhonetics("known", "noʊn", Array.Empty<string>()));
        _phonemeLibrary.GetWordPhonetics("unsupported").Returns((WordPhonetics?)null);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Keep going.");
        _tts.SynthesizeAsync("Keep going.", Arg.Any<CancellationToken>()).Returns(Speech());

        await CreateHandler().Handle(new SubmitUtteranceCommand(session.Id, new byte[] { 1 }), CancellationToken.None);

        await _practiceWords.Received(1).SaveAsync(
            Arg.Is<SpeakingPracticeWord>(word => word.Word == "known"),
            Arg.Any<CancellationToken>());
        await _practiceWords.DidNotReceive().SaveAsync(
            Arg.Is<SpeakingPracticeWord>(word => word.Word == "unsupported"),
            Arg.Any<CancellationToken>());
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public MutableTimeProvider(DateTimeOffset now) => Now = now;

        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static SynthesizedSpeech Speech() =>
        new(new byte[] { 9 },
            VisemeSequence.Create(new[] { new VisemeFrame(0, TimeSpan.Zero) }, TimeSpan.FromMilliseconds(80)),
            IsNaturalVoice: false);

    [Fact]
    public async Task Process_emits_tutor_before_pronunciation_and_audio_finish()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1);
        var audio = new byte[] { 1, 2, 3 };
        var pronunciationSource = new TaskCompletionSource<PronunciationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var speechSource = new TaskCompletionSource<SynthesizedSpeech>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tutorEmitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("hello there", 0.96));
        _assessor.AssessAsync(audio, "hello there", Arg.Any<CancellationToken>()).Returns(pronunciationSource.Task);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Hello! How are you today?");
        _tts.SynthesizeAsync("Hello! How are you today?", Arg.Any<CancellationToken>()).Returns(speechSource.Task);

        var processing = CreateHandler().ProcessAsync(
            new SubmitUtteranceCommand(session.Id, audio),
            (eventName, _) =>
            {
                events.Add(eventName);
                if (eventName == "tutor") tutorEmitted.TrySetResult();
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await tutorEmitted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        events.Should().ContainInOrder("recognized", "tutor");
        processing.IsCompleted.Should().BeFalse();

        pronunciationSource.SetResult(new PronunciationResult(90, 90, 90, 90, Array.Empty<WordPronunciation>()));
        speechSource.SetResult(Speech());
        await processing;

        events.Should().ContainInOrder("recognized", "tutor", "pronunciation", "audio");
    }

    [Fact]
    public async Task Process_keeps_the_tutor_reply_when_pronunciation_assessment_fails()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel");
        var audio = new byte[] { 1, 2, 3 };
        var events = new List<string>();

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Accepted("I enjoy travelling by train.", 0.96));
        _assessor.AssessAsync(audio, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<PronunciationResult>>(_ => throw new InvalidOperationException("Malformed assessment payload"));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>())
            .Returns("Do you like sitting by the window?");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        await CreateHandler().ProcessAsync(
            new SubmitUtteranceCommand(session.Id, audio),
            (eventName, _) =>
            {
                events.Add(eventName);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        events.Should().ContainInOrder("recognized", "tutor", "audio");
        events.Should().NotContain("pronunciation");
        session.Turns.Should().HaveCount(2);
        session.Turns[0].Pronunciation.Should().BeNull();
        session.Turns[1].Text.Should().Be("Do you like sitting by the window?");
        await _conversations.Received(2).SaveAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Process_retries_a_pending_recognized_turn_without_adding_it_twice()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "family");
        session.AddLearnerTurn("She always wakes up at five.");
        var audio = new byte[] { 1, 2, 3 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Accepted("She always wakes up at five.", 0.96));
        _assessor.AssessAsync(audio, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>())
            .Returns("Five is quite early. What does she do first?");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        await CreateHandler().ProcessAsync(
            new SubmitUtteranceCommand(session.Id, audio),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        session.Turns.Count(turn => turn.Role == ConversationRole.Learner).Should().Be(1);
        session.Turns.Select(turn => turn.Role).Should().Equal(
            ConversationRole.Learner,
            ConversationRole.Tutor);
    }

    [Fact]
    public async Task Process_serializes_topic_and_daily_progress_persistence()
    {
        var topicId = Guid.NewGuid();
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1, vocabularyTopicId: topicId);
        session.RecordSpokenActivity(Now);
        _clock.Now = Now.AddMinutes(1);
        var audio = new byte[] { 4, 5, 6 };
        var topicProgressStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseTopicProgress = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistenceOrder = new List<string>();

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("I enjoy travelling", 0.96));
        _assessor.AssessAsync(audio, "I enjoy travelling", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Where would you like to travel?");
        _tts.SynthesizeAsync("Where would you like to travel?", Arg.Any<CancellationToken>()).Returns(Speech());
        _topicProgress.GetAsync(session.LearnerId, topicId, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            persistenceOrder.Add("topic");
            topicProgressStarted.TrySetResult();
            return WaitForTopicProgressAsync();
        });
        _dailyProgress.RecordSkillAsync(
                session.LearnerId, SkillType.Speaking, 75, Now.AddMinutes(1), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                persistenceOrder.Add("daily");
                return SkillRewardDto.NotAwarded();
            });

        var processing = CreateHandler().ProcessAsync(
            new SubmitUtteranceCommand(session.Id, audio),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        await topicProgressStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        persistenceOrder.Should().Equal("topic");

        releaseTopicProgress.SetResult();
        await processing;
        persistenceOrder.Should().Equal("topic", "daily");

        async Task<TopicSpeakingProgress?> WaitForTopicProgressAsync()
        {
            await releaseTopicProgress.Task;
            return null;
        }
    }

    [Fact]
    public async Task Handle_runs_full_pipeline_and_returns_uzbek_feedback()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1);
        var audio = new byte[] { 1, 2, 3, 4 };
        var pronunciation = new PronunciationResult(72, 70, 75, 100, new[]
        {
            new WordPronunciation("good", 92, PronunciationErrorType.None),
            new WordPronunciation("three", 45, PronunciationErrorType.Mispronunciation)
        });

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("good three", 0.96));
        _assessor.AssessAsync(audio, "good three", Arg.Any<CancellationToken>()).Returns(pronunciation);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns(call =>
        {
            var currentSession = call.Arg<ConversationSession>();
            currentSession.Turns.Should().ContainSingle();
            currentSession.Turns[^1].Role.Should().Be(ConversationRole.Learner);
            currentSession.Turns[^1].Text.Should().Be("good three");
            currentSession.Turns[^1].Pronunciation.Should().BeNull();
            return "Nice try, keep going!";
        });
        _tts.SynthesizeAsync("Nice try, keep going!", Arg.Any<CancellationToken>()).Returns(Speech());
        _feedback.Get("pron.mispronunciation",
                Arg.Is<IReadOnlyDictionary<string, string>>(d => d["word"] == "three"))
            .Returns("\"three\" so'zini qayta mashq qiling.");

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.RecognizedText.Should().Be("good three");
        result.TutorText.Should().Be("Nice try, keep going!");
        result.Pronunciation!.Band.Should().Be(PronunciationBand.NeedsImprovement);
        result.FocusWord.Should().Be("three");
        result.FeedbackUz.Should().Be("\"three\" so'zini qayta mashq qiling.");
        result.TopicProgress.Should().BeNull(); // free conversation - no linked topic
        result.IsNaturalVoice.Should().BeFalse();
        session.Turns.Should().HaveCount(2); // learner + tutor
        session.Turns[0].Pronunciation.Should().BeSameAs(pronunciation);
        await _conversations.Received(2).SaveAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_runs_pronunciation_and_tutor_generation_in_parallel_after_stt()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1);
        var audio = new byte[] { 7, 8, 9 };
        var pronunciationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tutorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRemoteWork = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("I enjoy travelling", 0.96));
        _assessor.AssessAsync(audio, "I enjoy travelling", Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            pronunciationStarted.SetResult();
            await releaseRemoteWork.Task;
            return new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>());
        });
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            tutorStarted.SetResult();
            await releaseRemoteWork.Task;
            return "Where would you like to travel?";
        });
        _tts.SynthesizeAsync("Where would you like to travel?", Arg.Any<CancellationToken>()).Returns(Speech());

        var handleTask = CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        await Task.WhenAll(pronunciationStarted.Task, tutorStarted.Task).WaitAsync(TimeSpan.FromSeconds(2));
        releaseRemoteWork.SetResult();
        var result = await handleTask;

        result.TutorText.Should().Be("Where would you like to travel?");
        result.Pronunciation.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_marks_topic_learned_after_five_minutes_of_speaking()
    {
        // A conversation linked to a vocabulary topic: speaking about it for 5 minutes (the
        // accumulated goal) marks the topic learned and flags it as just-learned once.
        var topic = VocabularyTopic.Curate(
            "b1-at-the-market", "At the Market", "Bozorda", "shopping", "present-simple", CefrLevel.B1, Now);
        var topicId = topic.Id;
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.B1, vocabularyTopicId: topicId);
        // Establish the speaking baseline at the earlier instant so this utterance counts a 2-minute gap.
        session.RecordSpokenActivity(Now);
        _clock.Now = Now.AddMinutes(2);

        // Already 4:50 of practice from earlier turns; this 2-minute utterance crosses the 5-minute goal.
        var existing = TopicSpeakingProgress.Start(session.LearnerId, topicId, Now);
        existing.AddSpeaking(TimeSpan.FromSeconds(290), Now);
        _topicProgress.GetAsync(session.LearnerId, topicId, Arg.Any<CancellationToken>()).Returns(existing);
        _topics.GetByIdAsync(topicId, Arg.Any<CancellationToken>()).Returns(topic);
        var completion = TopicCompletionRecord.Start(session.LearnerId, topicId, topic.Level, Now);
        foreach (var module in TopicCompletionRecord.RequiredModules.TakeWhile(module => module != SkillType.Speaking))
            completion.RecordModule(module, 90, Now);
        _completions.GetAsync(session.LearnerId, topicId, Arg.Any<CancellationToken>()).Returns(completion);

        var audio = new byte[] { 5 };
        var pronunciation = new PronunciationResult(95, 95, 92, 100, new[]
        {
            new WordPronunciation("market", 95, PronunciationErrorType.None)
        });
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("at the market", 0.96));
        _assessor.AssessAsync(audio, "at the market", Arg.Any<CancellationToken>()).Returns(pronunciation);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Nice!");
        _tts.SynthesizeAsync("Nice!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.TopicProgress.Should().NotBeNull();
        result.TopicProgress!.GoalSeconds.Should().Be(300);
        result.TopicProgress.Learned.Should().BeTrue();
        result.TopicProgress.JustLearned.Should().BeTrue();
        result.TopicProgress.SpokenSeconds.Should().Be(410); // 290 + 120

        // The learned progress is staged twice - once from `UpdateTopicProgressAsync`, once from
        // `CreditTopicCompletionAsync` after recording the speaking session evidence - but the turn
        // commits everything exactly once, so a failure cannot credit spoken time without the
        // module score it earned.
        await _topicProgress.Received(2).TrackAsync(
            Arg.Is<TopicSpeakingProgress>(p => p.IsLearned), Arg.Any<CancellationToken>());
        await _topicProgress.DidNotReceive().SaveAsync(
            Arg.Any<TopicSpeakingProgress>(), Arg.Any<CancellationToken>());
        await _completions.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        // Crossing the 5-minute goal credits the topic's Speaking module (K.5) and returns the
        // six-skill checklist so the client can show the full skill overview.
        result.Completion.Should().NotBeNull();
        result.Completion!.Modules.Single(m => m.Module == "Speaking").Score.Should().Be(95);
        result.Completion.Modules.Single(m => m.Module == "Speaking").Passed.Should().BeTrue();
        result.Completion.Modules.Single(m => m.Module == "Listening").Unlocked.Should().BeTrue();
        await _completions.Received(1).TrackAsync(
            Arg.Is<TopicCompletionRecord>(r => r.IsModulePassed(SkillType.Speaking)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_unlocks_listening_after_speaking_is_attempted_below_mastery_threshold()
    {
        var topic = VocabularyTopic.Curate(
            "b1-travel", "Travel", "Sayohat", "travel", "past-simple", CefrLevel.B1, Now);
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.B1, vocabularyTopicId: topic.Id);
        session.RecordSpokenActivity(Now);
        _clock.Now = Now.AddMinutes(2);

        var progress = TopicSpeakingProgress.Start(session.LearnerId, topic.Id, Now);
        progress.AddSpeaking(TimeSpan.FromSeconds(290), Now);
        _topicProgress.GetAsync(session.LearnerId, topic.Id, Arg.Any<CancellationToken>()).Returns(progress);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var completion = TopicCompletionRecord.Start(session.LearnerId, topic.Id, topic.Level, Now);
        foreach (var module in TopicCompletionRecord.RequiredModules.TakeWhile(module => module != SkillType.Speaking))
            completion.RecordModule(module, 90, Now);
        _completions.GetAsync(session.LearnerId, topic.Id, Arg.Any<CancellationToken>()).Returns(completion);

        var audio = new byte[] { 5 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("I travelled last year", 0.96));
        _assessor.AssessAsync(audio, "I travelled last year", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(74, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Nice try!");
        _tts.SynthesizeAsync("Nice try!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.Completion.Should().NotBeNull();
        result.Completion!.Modules.Single(m => m.Module == "Speaking").Score.Should().Be(74);
        result.Completion.Modules.Single(m => m.Module == "Speaking").Passed.Should().BeFalse();
        result.Completion.Modules.Single(m => m.Module == "Listening").Unlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_does_not_credit_speaking_module_before_the_goal_is_reached()
    {
        // A linked-topic conversation still short of the 5-minute goal: the Speaking module stays
        // unpassed, but the checklist is still returned so the client can show what's left.
        var topic = VocabularyTopic.Curate(
            "a2-daily-routine", "Daily Routine", "Kunlik tartib", "habits", "present-simple", CefrLevel.A2, Now);
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, vocabularyTopicId: topic.Id);
        session.RecordSpokenActivity(Now);
        _clock.Now = Now.AddMinutes(1);

        var existing = TopicSpeakingProgress.Start(session.LearnerId, topic.Id, Now);
        existing.AddSpeaking(TimeSpan.FromSeconds(60), Now);
        _topicProgress.GetAsync(session.LearnerId, topic.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _completions.GetAsync(session.LearnerId, topic.Id, Arg.Any<CancellationToken>())
            .Returns((TopicCompletionRecord?)null);

        var audio = new byte[] { 5 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("I wake up early", 0.96));
        _assessor.AssessAsync(audio, "I wake up early", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, new[]
            {
                new WordPronunciation("early", 90, PronunciationErrorType.None)
            }));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Nice!");
        _tts.SynthesizeAsync("Nice!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.TopicProgress!.Learned.Should().BeFalse();
        result.Completion.Should().NotBeNull();
        result.Completion!.Modules.Single(m => m.Module == "Speaking").Passed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_reports_topic_progress_without_crediting_time_on_the_first_utterance()
    {
        // The first utterance of a topic conversation only sets the speaking baseline, so no time
        // is credited yet (delta is zero) and nothing is persisted - but progress is still reported.
        var topicId = Guid.NewGuid();
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, vocabularyTopicId: topicId);
        _topicProgress.GetAsync(session.LearnerId, topicId, Arg.Any<CancellationToken>())
            .Returns((TopicSpeakingProgress?)null);

        var audio = new byte[] { 5 };
        var pronunciation = new PronunciationResult(95, 95, 92, 100, new[]
        {
            new WordPronunciation("hello", 95, PronunciationErrorType.None)
        });
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("hello", 0.96));
        _assessor.AssessAsync(audio, "hello", Arg.Any<CancellationToken>()).Returns(pronunciation);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Hi!");
        _tts.SynthesizeAsync("Hi!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.TopicProgress.Should().NotBeNull();
        result.TopicProgress!.SpokenSeconds.Should().Be(0);
        result.TopicProgress.Learned.Should().BeFalse();
        result.TopicProgress.JustLearned.Should().BeFalse();
        await _topicProgress.DidNotReceive().TrackAsync(
            Arg.Any<TopicSpeakingProgress>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_uses_good_feedback_when_no_word_needs_practice()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var audio = new byte[] { 5 };
        var pronunciation = new PronunciationResult(95, 95, 92, 100, new[]
        {
            new WordPronunciation("hello", 95, PronunciationErrorType.None)
        });

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("hello", 0.96));
        _assessor.AssessAsync(audio, "hello", Arg.Any<CancellationToken>()).Returns(pronunciation);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Great!");
        _tts.SynthesizeAsync("Great!", Arg.Any<CancellationToken>()).Returns(Speech());
        _feedback.Get("pron.good", null).Returns("Ajoyib! Talaffuzingiz aniq.");

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.FocusWord.Should().BeNull();
        result.FeedbackUz.Should().Be("Ajoyib! Talaffuzingiz aniq.");
    }

    [Fact]
    public async Task Handle_returns_unrecognized_without_recording_a_turn_when_stt_is_empty()
    {
        // Silence / unintelligible audio: STT yields nothing. The handler must NOT add
        // an empty learner turn (a domain invariant violation -> 409) - it returns a
        // "not recognized" result so the client can prompt a retry.
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var audio = new byte[] { 1 };

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech));
        _feedback.Get("pron.not_recognized").Returns("Ovoz aniqlanmadi.");

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.Recognized.Should().BeFalse();
        result.RecognizedText.Should().BeEmpty();
        result.Pronunciation.Should().BeNull();
        result.FeedbackUz.Should().Be("Ovoz aniqlanmadi.");
        result.RejectionCode.Should().Be("no_speech");
        session.Turns.Should().BeEmpty();
        await _conversations.DidNotReceive().SaveAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
        await _assessor.DidNotReceive().AssessAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(SpeechTranscriptionRejection.InvalidAudio)]
    [InlineData(SpeechTranscriptionRejection.LowConfidence)]
    public async Task Handle_rejects_untrusted_transcripts_without_running_downstream_work(
        SpeechTranscriptionRejection rejection)
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var audio = new byte[] { 1 };

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Rejected(rejection));
        _feedback.Get("pron.not_recognized").Returns("Ovoz aniqlanmadi.");

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.Recognized.Should().BeFalse();
        result.RejectionCode.Should().Be(
            rejection == SpeechTranscriptionRejection.InvalidAudio
                ? "invalid_audio"
                : "low_confidence");
        session.Turns.Should().BeEmpty();
        await _conversations.DidNotReceive().SaveAsync(
            Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
        await _assessor.DidNotReceive().AssessAsync(
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _tutor.DidNotReceive().NextReplyAsync(
            Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_marks_result_recognized_on_success()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var audio = new byte[] { 5 };
        var pronunciation = new PronunciationResult(95, 95, 92, 100, new[]
        {
            new WordPronunciation("hello", 95, PronunciationErrorType.None)
        });

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("hello", 0.96));
        _assessor.AssessAsync(audio, "hello", Arg.Any<CancellationToken>()).Returns(pronunciation);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Great!");
        _tts.SynthesizeAsync("Great!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.Recognized.Should().BeTrue();
        result.Pronunciation.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_normalizes_time_for_assessment_and_returns_the_original_token()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var audio = new byte[] { 5, 0, 0 };
        var pronunciation = new PronunciationResult(65, 65, 70, 100, new[]
        {
            new WordPronunciation("five", 60, PronunciationErrorType.Mispronunciation),
            new WordPronunciation("AM", 90, PronunciationErrorType.None),
        });

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("I wake up at 5:00 AM", 0.96));
        _assessor.AssessAsync(audio, "I wake up at five AM", Arg.Any<CancellationToken>()).Returns(pronunciation);
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("That is an early start.");
        _tts.SynthesizeAsync("That is an early start.", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.RecognizedText.Should().Be("I wake up at 5:00 AM");
        result.Pronunciation!.Words.Should().ContainSingle();
        result.Pronunciation.Words[0].Word.Should().Be("5:00 AM");
        result.Pronunciation.Words[0].SpokenForm.Should().Be("five AM");
    }

    [Fact]
    public async Task Handle_refuses_the_turn_without_running_the_pipeline_once_the_speaking_cap_is_reached()
    {
        // A conversation that already hit the 10-minute engaged-speaking cap must not spend any more
        // of the paid pipeline (STT/assessment/tutor/TTS) - it returns the end-of-session signal.
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        SpeakToTheCap(session);
        session.HasReachedSpeakingLimit.Should().BeTrue();

        var audio = new byte[] { 5 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.SessionLimitReached.Should().BeTrue();
        result.Recognized.Should().BeFalse();
        await _stt.DidNotReceive().TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await _assessor.DidNotReceive().AssessAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _tts.DidNotReceive().SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _conversations.DidNotReceive().SaveAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_flags_session_limit_reached_on_the_turn_that_crosses_the_cap()
    {
        // The utterance that pushes engaged-speaking time over the cap still completes normally, but
        // signals the client to end the sitting afterward.
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        // One short of the cap, then this 2-minute utterance crosses it.
        session.RecordSpokenActivity(Now);
        var elapsed = TimeSpan.Zero;
        while (session.SpokenTime < ConversationSession.MaxSpokenTime - ConversationSession.MaxCountedGap)
        {
            elapsed += ConversationSession.MaxCountedGap;
            session.RecordSpokenActivity(Now + elapsed);
        }
        _clock.Now = Now + elapsed + ConversationSession.MaxCountedGap;

        var audio = new byte[] { 5 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("one more thing", 0.96));
        _assessor.AssessAsync(audio, "one more thing", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, new[]
            {
                new WordPronunciation("thing", 90, PronunciationErrorType.None)
            }));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Great chat!");
        _tts.SynthesizeAsync("Great chat!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.Recognized.Should().BeTrue();
        result.SessionLimitReached.Should().BeTrue();
    }

    // Accumulates engaged-speaking time in capped steps until the per-conversation cap is reached.
    private static void SpeakToTheCap(ConversationSession session)
    {
        session.RecordSpokenActivity(Now);
        var elapsed = TimeSpan.Zero;
        while (session.SpokenTime < ConversationSession.MaxSpokenTime)
        {
            elapsed += ConversationSession.MaxCountedGap;
            session.RecordSpokenActivity(Now + elapsed);
        }
    }

    [Fact]
    public async Task Handle_throws_when_session_not_found()
    {
        _conversations.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ConversationSession?)null);

        var act = () => CreateHandler().Handle(
            new SubmitUtteranceCommand(Guid.NewGuid(), new byte[] { 1 }), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_flags_name_prompt_when_learner_introduces_name_and_none_is_stored()
    {
        // STT garbles a spoken name, the account has no preferred name yet, and the learner clearly
        // tried to introduce themselves - the client should surface the type-your-name input.
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var audio = new byte[] { 1, 2 };

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("my name is Java whir", 0.96));
        _assessor.AssessAsync(audio, "my name is Java whir", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(80, 80, 80, 100, new[]
            {
                new WordPronunciation("name", 90, PronunciationErrorType.None)
            }));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Nice to meet you!");
        _tts.SynthesizeAsync("Nice to meet you!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.NamePrompt.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_does_not_flag_name_prompt_once_a_name_is_stored()
    {
        // The learner already set a preferred name (refreshed onto the session here): re-introducing
        // themselves must not re-open the name input, and the tutor keeps the stored name.
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2);
        var account = UserAccount.Register("sub-1", "a@b.com", "Aziz", null, Now);
        account.SetPreferredName("Javohir");
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns(account);
        var audio = new byte[] { 3, 4 };

        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>()).Returns(SpeechTranscription.Accepted("my name is Javohir", 0.96));
        _assessor.AssessAsync(audio, "my name is Javohir", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(80, 80, 80, 100, new[]
            {
                new WordPronunciation("name", 90, PronunciationErrorType.None)
            }));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Good to see you again!");
        _tts.SynthesizeAsync("Good to see you again!", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        result.NamePrompt.Should().BeFalse();
        session.LearnerName.Should().Be("Javohir"); // refreshed from the account mid-conversation
    }

    [Fact]
    public async Task Handle_returns_confirmation_candidates_without_mutating_the_session()
    {
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        session.AddTutorTurn("What do you prefer in the morning?");
        var audio = new byte[] { 1, 2, 3 };
        var contextualStt = Substitute.For<IContextualSpeechToTextService>();
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        contextualStt.TranscribeAsync(
                audio,
                Arg.Any<SpeechRecognitionContext>(),
                Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Accepted(
                "I prefer tea in the morning.",
                0.71,
                new[]
                {
                    new SpeechTranscriptionCandidate("I prefer tea in the morning.", 0.71),
                    new SpeechTranscriptionCandidate("I prefer tea in Jammu.", 0.70),
                },
                requiresConfirmation: true,
                scoreGap: 0.04));

        var handler = CreateHandler(contextualStt);
        var result = await handler.Handle(
            new SubmitUtteranceCommand(session.Id, audio),
            CancellationToken.None);

        result.Recognized.Should().BeFalse();
        result.RequiresTranscriptConfirmation.Should().BeTrue();
        result.TranscriptAlternatives.Should().Contain(candidate =>
            candidate.Text == "I prefer tea in the morning.");
        session.LearnerTurnCount.Should().Be(0);
        await _assessor.DidNotReceive().AssessAsync(
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _tutor.DidNotReceive().NextReplyAsync(
            Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_uses_confirmed_transcript_for_tutor_and_pronunciation_without_rerunning_stt()
    {
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        var audio = new byte[] { 1, 2, 3 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _assessor.AssessAsync(audio, "I prefer tea in the morning.", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(88, 88, 84, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Tea is a nice morning drink.");
        _tts.SynthesizeAsync("Tea is a nice morning drink.", Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new SubmitUtteranceCommand(
                session.Id,
                audio,
                "  I prefer tea in the morning.  ",
                "edited"),
            CancellationToken.None);

        result.Recognized.Should().BeTrue();
        result.RecognizedText.Should().Be("I prefer tea in the morning.");
        session.LastTurn.Should().NotBeNull();
        session.Turns.Should().Contain(turn =>
            turn.Role == ConversationRole.Learner
            && turn.Text == "I prefer tea in the morning.");
        await _stt.DidNotReceive().TranscribeAsync(
            Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await _assessor.Received(1).AssessAsync(
            audio,
            "I prefer tea in the morning.",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_samples_pronunciation_instead_of_assessing_every_turn()
    {
        // Pronunciation assessment re-bills the same audio speech-to-text already charged for, so a
        // conversation scores the opening turns and then every AssessedTurnInterval-th turn.
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        var audio = new byte[] { 1, 2, 3 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _assessor.AssessAsync(audio, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Tell me more.");
        _tts.SynthesizeAsync("Tell me more.", Arg.Any<CancellationToken>()).Returns(Speech());

        var handler = CreateHandler();
        for (var turn = 1; turn <= 5; turn++)
        {
            await handler.Handle(
                new SubmitUtteranceCommand(session.Id, audio, $"utterance number {turn}", "edited"),
                CancellationToken.None);
        }

        session.LearnerTurnCount.Should().Be(5);
        // Turns 1, 2 (opening) and 4 (interval) are scored; turns 3 and 5 are not.
        await _assessor.Received(3).AssessAsync(
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_does_not_book_a_zero_speaking_score_when_no_turn_was_assessed()
    {
        var learnerId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var session = ConversationSession.Start(
            learnerId, CefrLevel.A2, topic: "Travel", vocabularyTopicId: topicId);
        var audio = new byte[] { 1, 2, 3 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        // Every assessment fails, so no learner turn carries a score.
        _assessor.AssessAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<PronunciationResult>(_ => throw new InvalidOperationException("assessor down"));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Tell me more.");
        _tts.SynthesizeAsync("Tell me more.", Arg.Any<CancellationToken>()).Returns(Speech());

        await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio, "I travelled last year", "edited"),
            CancellationToken.None);

        // A fabricated zero would wrongly mark the learner as failing the speaking module.
        await _topicProgress.DidNotReceive().TrackAsync(
            Arg.Any<TopicSpeakingProgress>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_continues_a_pending_learner_turn_even_when_pronunciation_was_skipped()
    {
        // A tutor timeout leaves the learner turn as the last turn. Retrying the same audio must
        // continue it rather than appending a duplicate - independently of whether that turn was
        // sampled for pronunciation.
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        session.AddLearnerTurn("I wake up early");
        var audio = new byte[] { 1, 2, 3 };
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _assessor.AssessAsync(audio, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Nice, what next?");
        _tts.SynthesizeAsync("Nice, what next?", Arg.Any<CancellationToken>()).Returns(Speech());

        await CreateHandler().Handle(
            new SubmitUtteranceCommand(session.Id, audio, "I wake up early", "edited"),
            CancellationToken.None);

        session.LearnerTurnCount.Should().Be(1);
        session.Turns.Should().HaveCount(2);
        session.LastTurn!.Role.Should().Be(ConversationRole.Tutor);
    }

    private SubmitUtteranceCommandHandler CreateHandler(ISpeechToTextService stt) =>
        new(_conversations, stt, _assessor, _tutor, _tts, _feedback, _topicProgress, _topics, _completions, _dailyProgress, _accounts, _events, _clock, _practiceWords, _phonemeLibrary, curriculum: _curriculum);

    private SubmitUtteranceCommandHandler CreateHandler(ISpeakingMinuteAllowanceService allowance) =>
        new(_conversations, _stt, _assessor, _tutor, _tts, _feedback, _topicProgress, _topics, _completions, _dailyProgress, _accounts, _events, _clock, _practiceWords, _phonemeLibrary, curriculum: _curriculum, speakingMinutes: allowance);

    [Fact]
    public async Task An_exhausted_daily_allowance_blocks_the_turn_before_it_reaches_speech_to_text()
    {
        // The whole point of the budget is not to pay Azure for the transcription, so the gate has
        // to run before the audio is sent - not after.
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        var allowance = Substitute.For<ISpeakingMinuteAllowanceService>();
        allowance.EnsureAllowedAsync(learnerId, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new SpeakingMinutesExhaustedException(5, 5, Now.AddHours(4)));

        var act = () => CreateHandler(allowance).Handle(
            new SubmitUtteranceCommand(session.Id, new byte[] { 1, 2, 3 }), CancellationToken.None);

        await act.Should().ThrowAsync<SpeakingMinutesExhaustedException>();
        await _stt.DidNotReceive().TranscribeAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await _tutor.DidNotReceive().NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Speech_is_booked_only_after_the_transcript_is_accepted()
    {
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        // One second of 16 kHz 16-bit mono PCM.
        var audio = new byte[32_000];
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Accepted("I wake up early", 0.96));
        _assessor.AssessAsync(audio, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Nice, what next?");
        _tts.SynthesizeAsync("Nice, what next?", Arg.Any<CancellationToken>()).Returns(Speech());
        var allowance = Substitute.For<ISpeakingMinuteAllowanceService>();
        allowance.RecordAsync(learnerId, Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(new SpeakingMinuteDecision(true, 5, 1, 4));

        await CreateHandler(allowance).Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        await allowance.Received(1).RecordAsync(
            learnerId,
            Arg.Is<double>(minutes => Math.Abs(minutes - 1d / 60d) < 0.001),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_rejected_clip_does_not_consume_the_learners_allowance()
    {
        // Azure billed us either way, but taking the learner's budget for audio the recognizer threw
        // away would punish them for a microphone problem.
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        var audio = new byte[32_000];
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _stt.TranscribeAsync(audio, Arg.Any<CancellationToken>())
            .Returns(SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech));
        var allowance = Substitute.For<ISpeakingMinuteAllowanceService>();

        await CreateHandler(allowance).Handle(
            new SubmitUtteranceCommand(session.Id, audio), CancellationToken.None);

        await allowance.DidNotReceive().RecordAsync(
            Arg.Any<Guid>(), Arg.Any<double>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_confirmed_transcript_is_neither_gated_nor_billed_because_it_skips_recognition()
    {
        var learnerId = Guid.NewGuid();
        var session = ConversationSession.Start(learnerId, CefrLevel.A2, topic: "daily routine");
        var audio = new byte[32_000];
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        _assessor.AssessAsync(audio, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(90, 90, 90, 100, Array.Empty<WordPronunciation>()));
        _tutor.NextReplyAsync(session, Arg.Any<CancellationToken>()).Returns("Good.");
        _tts.SynthesizeAsync("Good.", Arg.Any<CancellationToken>()).Returns(Speech());
        var allowance = Substitute.For<ISpeakingMinuteAllowanceService>();

        await CreateHandler(allowance).Handle(
            new SubmitUtteranceCommand(session.Id, audio, "I wake up early", "edited"),
            CancellationToken.None);

        await allowance.DidNotReceive().EnsureAllowedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await allowance.DidNotReceive().RecordAsync(
            Arg.Any<Guid>(), Arg.Any<double>(), Arg.Any<CancellationToken>());
    }
}
