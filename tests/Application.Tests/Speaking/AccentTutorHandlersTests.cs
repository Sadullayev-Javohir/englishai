using Application.Speaking.AccentTutors;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Speaking;
using Application.Common;
using Domain.Speaking;
using FluentAssertions;

namespace Application.Tests.Speaking;

public sealed class AccentTutorHandlersTests
{
    [Theory]
    [InlineData("american", "en-US")]
    [InlineData("british", "en-GB")]
    [InlineData("australian", "en-AU")]
    [InlineData("irish", "en-IE")]
    public void Recognition_context_uses_the_selected_accent_locale(string tutorId, string locale)
    {
        var context = AccentTutorRecognitionContext.Create(
            tutorId,
            new[] { new AccentTutorMessage("tutor", "Tell me about your morning.") });

        context.RecognitionLanguage.Should().Be(locale);
        context.LastTutorPrompt.Should().Be("Tell me about your morning.");
    }

    [Fact]
    public async Task Start_returns_agent_greeting_and_voice()
    {
        var handler = new AccentTutorStartQueryHandler(new Agent(), new Voice());

        var result = await handler.Handle(new AccentTutorStartQuery("american"), default);

        result.Text.Should().Be("Hey there! What have you been up to today?");
        result.TutorAudioBase64.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Start_reports_disabled_agent_as_non_retryable_configuration_error()
    {
        var handler = new AccentTutorStartQueryHandler(new DisabledAgent(), new Voice());

        var action = () => handler.Handle(new AccentTutorStartQuery("american"), default);

        await action.Should().ThrowAsync<SpeakingTutorUnavailableException>()
            .Where(exception =>
                exception.Code == "accent_tutor_not_configured" &&
                !exception.Retryable);
    }

    [Fact]
    public async Task Turn_transcribes_assesses_and_preserves_recent_history()
    {
        var agent = new Agent();
        var handler = new AccentTutorTurnCommandHandler(
            agent,
            new Stt(),
            new Assessor(),
            new Voice());

        var result = await handler.Handle(new AccentTutorTurnCommand(
            "british",
            [1, 2, 3],
            [new AccentTutorMessage("tutor", "Welcome."), new AccentTutorMessage("learner", "Hello.")]), default);

        result.Transcript.Should().Be("I went to the library yesterday.");
        result.Pronunciation.Words.Should().ContainSingle(word => word.Word == "library" && word.NeedsPractice);
        agent.History.Should().HaveCount(2);
    }

    [Fact]
    public async Task Turn_stream_emits_recognition_before_tutor_and_audio()
    {
        var handler = new AccentTutorTurnCommandHandler(
            new Agent(),
            new Stt(),
            new Assessor(),
            new Voice());
        var events = new List<string>();

        await handler.ProcessAsync(
            new AccentTutorTurnCommand("american", [1, 2, 3]),
            (name, _) => { events.Add(name); return Task.CompletedTask; },
            default);

        events.Should().ContainInOrder("recognized", "tutor", "pronunciation", "audio", "completed");
    }

    [Fact]
    public async Task Turn_saves_mispronounced_words_for_the_authenticated_learner()
    {
        var learnerId = Guid.NewGuid();
        var repository = new PracticeWords();
        var handler = new AccentTutorTurnCommandHandler(
            new Agent(), new Stt(), new Assessor(), new Voice(),
            repository, new CurrentUser(learnerId), TimeProvider.System);

        await handler.Handle(new AccentTutorTurnCommand("american", [1, 2, 3]), default);

        repository.Saved.Should().ContainSingle(word =>
            word.LearnerId == learnerId && word.NormalizedWord == "library");
    }

    [Fact]
    public async Task Nudge_asks_a_natural_follow_up_without_silence_language()
    {
        var agent = new Agent();
        var handler = new AccentTutorNudgeQueryHandler(agent, new Voice());

        await handler.Handle(new AccentTutorNudgeQuery("irish", [new("tutor", "Tell me about your trip.")]), default);

        agent.LearnerText.Should().Contain("Re-engage the learner naturally");
        agent.LearnerText.Should().Contain("Never mention silence");
    }

    [Fact]
    public async Task Turn_supplies_cefr_and_recent_conversation_hints_to_contextual_stt()
    {
        var stt = new ContextualStt();
        var handler = new AccentTutorTurnCommandHandler(speechToText: stt, agent: new Agent(), pronunciation: new Assessor(), voice: new Voice());

        await handler.Handle(new AccentTutorTurnCommand(
            "american",
            [1, 2, 3],
            [new("tutor", "What is your English level?")]), default);

        stt.Context.Should().NotBeNull();
        stt.Context!.LastTutorPrompt.Should().Be("What is your English level?");
        stt.Context.ContextPhrases.Should().Contain("My English level is A1");
        stt.Context.FocusWords.Should().Contain("A one");
    }

    [Theory]
    [InlineData("", 0.95, false)]
    [InlineData("la", 0.90, false)]
    [InlineData("music playing", 0.55, false)]
    [InlineData("stop", 0.80, true)]
    [InlineData("I want to answer", 0.84, true)]
    public void Interruption_gate_accepts_only_clear_semantic_speech(string text, double confidence, bool expected)
    {
        var transcription = string.IsNullOrWhiteSpace(text)
            ? SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech)
            : SpeechTranscription.Accepted(text, confidence);

        InterruptionSpeechGate.IsAccepted(transcription).Should().Be(expected);
    }

    [Fact]
    public async Task Evaluation_returns_averaged_scores_and_ai_feedback()
    {
        var agent = new Agent();
        var handler = new AccentTutorEvaluateCommandHandler(agent);

        var result = await handler.Handle(new AccentTutorEvaluateCommand(
            "american",
            [new("tutor", "Hello"), new("learner", "I enjoy reading books")],
            [new(80, 76, 88, 90), new(84, 82, 86, 92)]), default);

        result.Evaluable.Should().BeTrue();
        result.OverallScore.Should().Be(82);
        result.StrongestSkill.Should().Be("Completeness");
        result.FocusSkill.Should().Be("Accuracy");
        agent.LearnerText.Should().Contain("live lesson has ended");
    }

    [Fact]
    public async Task Unknown_tutor_is_rejected()
    {
        var handler = new AccentTutorStartQueryHandler(new Agent(), new Voice());
        var act = () => handler.Handle(new AccentTutorStartQuery("unknown"), default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Voice_live_token_reserves_budget_before_minting_and_returns_the_session_id()
    {
        var meter = new SessionMeter();
        var handler = new GetAccentTutorVoiceLiveConnectionQueryHandler(new Broker(), meter);

        var result = await handler.Handle(new GetAccentTutorVoiceLiveConnectionQuery("british"), default);

        result.SessionId.Should().Be("session-1");
        result.MaxSessionSeconds.Should().Be(600);
        meter.Reserved.Should().Be("british");
        meter.Cancelled.Should().BeNull();
    }

    [Fact]
    public async Task Voice_live_token_refunds_the_reservation_when_azure_cannot_mint()
    {
        var meter = new SessionMeter();
        var handler = new GetAccentTutorVoiceLiveConnectionQueryHandler(new FailingBroker(), meter);

        var act = () => handler.Handle(new GetAccentTutorVoiceLiveConnectionQuery("british"), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        meter.Cancelled.Should().Be("session-1", "nothing was spent, so the learner must get it back");
    }

    [Fact]
    public async Task Voice_live_token_is_refused_when_the_tutor_is_not_configured()
    {
        var meter = new SessionMeter();
        var handler = new GetAccentTutorVoiceLiveConnectionQueryHandler(new Broker { IsConfigured = false }, meter);

        var act = () => handler.Handle(new GetAccentTutorVoiceLiveConnectionQuery("british"), default);

        await act.Should().ThrowAsync<SpeakingTutorUnavailableException>();
        meter.Reserved.Should().BeNull("an unavailable tutor must not consume the learner's allowance");
    }

    [Fact]
    public async Task Voice_live_completion_settles_the_reported_duration()
    {
        var meter = new SessionMeter();
        var handler = new CompleteAccentTutorVoiceLiveSessionCommandHandler(meter);

        var result = await handler.Handle(
            new CompleteAccentTutorVoiceLiveSessionCommand("british", "session-1", 42),
            default);

        result.BilledSeconds.Should().Be(42);
        meter.Settled.Should().Be("session-1");
    }

    [Fact]
    public async Task Voice_live_completion_rejects_an_unknown_tutor()
    {
        var handler = new CompleteAccentTutorVoiceLiveSessionCommandHandler(new SessionMeter());

        var act = () => handler.Handle(
            new CompleteAccentTutorVoiceLiveSessionCommand("klingon", "session-1", 42),
            default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private sealed class SessionMeter : IVoiceLiveSessionMeter
    {
        public string? Reserved { get; private set; }
        public string? Settled { get; private set; }
        public string? Cancelled { get; private set; }

        public Task<VoiceLiveSessionTicket> ReserveAsync(string tutorId, CancellationToken cancellationToken = default)
        {
            Reserved = tutorId;
            return Task.FromResult(new VoiceLiveSessionTicket("session-1", 600, 28));
        }

        public Task<VoiceLiveSessionSettlement> SettleAsync(
            string sessionId, double reportedSeconds, CancellationToken cancellationToken = default)
        {
            Settled = sessionId;
            return Task.FromResult(new VoiceLiveSessionSettlement(reportedSeconds, 28, false));
        }

        public Task CancelAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            Cancelled = sessionId;
            return Task.CompletedTask;
        }
    }

    private class Broker : IVoiceLiveConnectionBroker
    {
        public bool IsConfigured { get; init; } = true;

        public virtual Task<VoiceLiveConnectionResult> CreateAsync(
            string tutorId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new VoiceLiveConnectionResult(
                "wss://example/voice-live/realtime?api-version=x",
                "authorization",
                "Bearer token",
                DateTimeOffset.UtcNow.AddMinutes(30),
                new VoiceLiveSessionConfig("en-GB-Voice", "pcm16", "pcm16", 24_000, 500, "server_vad")));
    }

    private sealed class FailingBroker : Broker
    {
        public override Task<VoiceLiveConnectionResult> CreateAsync(
            string tutorId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Azure is unavailable.");
    }

    private sealed class Agent : IAccentTutorAgent
    {
        public bool IsConfigured => true;
        public IReadOnlyList<AccentTutorMessage> History { get; private set; } = [];
        public string LearnerText { get; private set; } = "";

        public Task<string> ReplyAsync(string tutorId, IReadOnlyList<AccentTutorMessage> history, string learnerText, CancellationToken cancellationToken = default)
        {
            History = history;
            LearnerText = learnerText;
            return Task.FromResult("Hey there! What have you been up to today?");
        }
    }

    private sealed class DisabledAgent : IAccentTutorAgent
    {
        public bool IsConfigured => false;

        public Task<string> ReplyAsync(
            string tutorId,
            IReadOnlyList<AccentTutorMessage> history,
            string learnerText,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("A disabled agent must not be called.");
    }

    private sealed record CurrentUser(Guid Id) : ICurrentUserAccessor
    {
        public Guid? LearnerId => Id;
    }

    private sealed class PracticeWords : ISpeakingPracticeWordRepository
    {
        public List<SpeakingPracticeWord> Saved { get; } = [];
        public Task<SpeakingPracticeWord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<SpeakingPracticeWord?>(null);
        public Task<SpeakingPracticeWord?> GetByLearnerAndWordAsync(Guid learnerId, string normalizedWord, CancellationToken cancellationToken = default) =>
            Task.FromResult(Saved.FirstOrDefault(word => word.LearnerId == learnerId && word.NormalizedWord == normalizedWord));
        public Task<IReadOnlyList<SpeakingPracticeWord>> GetActiveAsync(Guid learnerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SpeakingPracticeWord>>(Saved.Where(word => word.LearnerId == learnerId).ToList());
        public Task SaveAsync(SpeakingPracticeWord word, CancellationToken cancellationToken = default)
        {
            if (!Saved.Contains(word)) Saved.Add(word);
            return Task.CompletedTask;
        }
    }

    private sealed class Stt : ISpeechToTextService
    {
        public Task<SpeechTranscription> TranscribeAsync(byte[] audioContent, CancellationToken cancellationToken = default) =>
            Task.FromResult(SpeechTranscription.Accepted("I went to the library yesterday.", .95));
    }

    private sealed class ContextualStt : IContextualSpeechToTextService
    {
        public SpeechRecognitionContext? Context { get; private set; }
        public Task<SpeechTranscription> TranscribeAsync(byte[] audioContent, CancellationToken cancellationToken = default) =>
            Task.FromResult(SpeechTranscription.Accepted("My English level is A1.", .9));
        public Task<SpeechTranscription> TranscribeAsync(byte[] audioContent, SpeechRecognitionContext context, CancellationToken cancellationToken = default)
        {
            Context = context;
            return TranscribeAsync(audioContent, cancellationToken);
        }
    }

    private sealed class Assessor : IPronunciationAssessor
    {
        public Task<PronunciationResult> AssessAsync(byte[] audioContent, string referenceText, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PronunciationResult(
                82,
                80,
                85,
                100,
                [new WordPronunciation("library", 62, PronunciationErrorType.Mispronunciation)]));
    }

    private sealed class Voice : IAccentTutorVoiceService
    {
        public Task<SynthesizedSpeech> SynthesizeAsync(string tutorId, string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SynthesizedSpeech(
                [1, 2],
                VisemeSequence.Create([new VisemeFrame(0, TimeSpan.Zero)], TimeSpan.FromMilliseconds(100)),
                false));
    }
}
