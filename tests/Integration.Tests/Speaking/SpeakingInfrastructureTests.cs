using System.Net;
using System.Text;
using Application.Ai;
using Application.Speaking;
using Application.Speaking.Common;
using Application.Speaking.Ports;
using Domain.Assessment;
using Domain.Speaking;
using FluentAssertions;
using Infrastructure.Llm;
using Infrastructure.Speaking;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Speaking;

public class SpeakingInfrastructureTests
{
    [Fact]
    public void Azure_speech_default_confidence_accepts_accented_learner_speech()
    {
        var options = new AzureSpeechOptions();

        // Kept low so genuine Uzbek/L2 utterances scoring ~0.30-0.45 are accepted (for
        // confirmation) instead of silently rejected as "no reply".
        options.MinimumRecognitionConfidence.Should().Be(0.30);
        options.ConfirmationConfidenceThreshold.Should().Be(0.60);
        options.ConfirmationScoreGap.Should().Be(0.05);
    }

    [Fact]
    public void Contextual_selector_prefers_morning_over_a_proper_noun_for_a_morning_prompt()
    {
        var context = new SpeechRecognitionContext(
            "What do you prefer in the morning?",
            "daily routine",
            Array.Empty<string>(),
            new[] { "What do you prefer in the morning?", "daily routine" },
            IncludeNameHints: false);

        var selection = ContextualTranscriptSelector.Select(
            new[]
            {
                new SpeechTranscriptionCandidate("I prefer tea in Jammu.", 0.76),
                new SpeechTranscriptionCandidate("I prefer tea in the morning.", 0.72),
            },
            context,
            confirmationConfidenceThreshold: 0.60,
            confirmationScoreGap: 0.05,
            maximumAlternatives: 3);

        selection.Should().NotBeNull();
        selection!.Text.Should().Be("I prefer tea in the morning.");
        selection.RequiresConfirmation.Should().BeFalse();
    }

    [Theory]
    [InlineData("My English level is Iran.", "My English level is A1.")]
    [InlineData("My English level is A one.", "My English level is A1.")]
    [InlineData("My English level is A two.", "My English level is A2.")]
    [InlineData("I am B one.", "I am B1.")]
    [InlineData("I'm C two.", "I'm C2.")]
    public void English_level_context_safely_corrects_common_cefr_confusions(string recognized, string expected)
    {
        var context = new SpeechRecognitionContext(
            "What is your English level?",
            "English speaking lesson and CEFR level",
            Array.Empty<string>(),
            new[] { "My English level is A1", "A one", "A two", "B one", "C two" },
            IncludeNameHints: false);

        ContextualTranscriptCorrections.Apply(recognized, context).Should().Be(expected);
    }

    [Fact]
    public void Non_level_context_does_not_rewrite_the_country_iran()
    {
        var context = new SpeechRecognitionContext(
            "Which country are you from?", "travel", Array.Empty<string>(), Array.Empty<string>(), false);

        ContextualTranscriptCorrections.Apply("I am from Iran.", context).Should().Be("I am from Iran.");
    }

    [Fact]
    public void Recognition_context_only_enables_uzbek_names_when_the_tutor_asks_for_a_name()
    {
        var routine = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "daily routine");
        routine.AddTutorTurn("What do you prefer in the morning?");
        var introduction = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        introduction.AddTutorTurn("What is your name?");

        SpeechRecognitionContextFactory.FromSession(routine).IncludeNameHints.Should().BeFalse();
        SpeechRecognitionContextFactory.FromSession(introduction).IncludeNameHints.Should().BeTrue();
    }

    [Fact]
    public void Contextual_selector_auto_accepts_a_clear_high_confidence_winner()
    {
        var selection = ContextualTranscriptSelector.Select(
            new[]
            {
                new SpeechTranscriptionCandidate("I prefer tea in the morning.", 0.94),
                new SpeechTranscriptionCandidate("I prefer tea in Jammu.", 0.61),
            },
            new SpeechRecognitionContext(
                "What do you prefer in the morning?",
                "daily routine",
                Array.Empty<string>(),
                new[] { "morning", "daily routine" },
                IncludeNameHints: false),
            confirmationConfidenceThreshold: 0.60,
            confirmationScoreGap: 0.05,
            maximumAlternatives: 3);

        selection.Should().NotBeNull();
        selection!.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public void Contextual_selector_requires_confirmation_for_a_low_confidence_transcript()
    {
        var selection = ContextualTranscriptSelector.Select(
            new[]
            {
                new SpeechTranscriptionCandidate("I prefer tea in the morning.", 0.55),
            },
            new SpeechRecognitionContext(
                "What do you prefer in the morning?",
                "daily routine",
                Array.Empty<string>(),
                new[] { "morning", "daily routine" },
                IncludeNameHints: false),
            confirmationConfidenceThreshold: 0.60,
            confirmationScoreGap: 0.05,
            maximumAlternatives: 3);

        selection.Should().NotBeNull();
        selection!.RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public void Contextual_selector_requires_confirmation_for_two_nearly_equal_distinct_transcripts()
    {
        var selection = ContextualTranscriptSelector.Select(
            new[]
            {
                new SpeechTranscriptionCandidate("I prefer tea in the morning.", 0.71),
                new SpeechTranscriptionCandidate("I prefer tea in Jammu.", 0.70),
            },
            new SpeechRecognitionContext(
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                IncludeNameHints: false),
            confirmationConfidenceThreshold: 0.60,
            confirmationScoreGap: 0.05,
            maximumAlternatives: 3);

        selection.Should().NotBeNull();
        selection!.RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public void Streaming_transcript_falls_back_to_the_last_partial_when_no_final_segment_arrives()
    {
        // Reproduces the production failure: audio streamed, a partial was recognized, but the
        // stream ended (EndOfStream) before Azure committed a final Recognized segment. Instead
        // of "no speech" (which surfaced as "the tutor never replied"), the partial is used.
        var transcription = StreamingTranscriptComposer.Build(
            Array.Empty<IReadOnlyList<SpeechTranscriptionCandidate>>(),
            "I like playing football with my friends",
            EmptyRecognitionContext,
            new AzureSpeechOptions(),
            NullLogger.Instance);

        transcription.IsAccepted.Should().BeTrue();
        transcription.Text.Should().Be("I like playing football with my friends");
        transcription.RequiresConfirmation.Should().BeFalse();
    }

    [Fact]
    public void Streaming_transcript_prefers_a_final_segment_over_the_last_partial()
    {
        var transcription = StreamingTranscriptComposer.Build(
            new IReadOnlyList<SpeechTranscriptionCandidate>[]
            {
                new[] { new SpeechTranscriptionCandidate("I like tea in the morning.", 0.92) },
            },
            "stale partial hypothesis",
            EmptyRecognitionContext,
            new AzureSpeechOptions(),
            NullLogger.Instance);

        transcription.IsAccepted.Should().BeTrue();
        transcription.Text.Should().Be("I like tea in the morning.");
    }

    [Fact]
    public void Streaming_transcript_rejects_no_speech_without_a_final_segment_or_partial()
    {
        var transcription = StreamingTranscriptComposer.Build(
            Array.Empty<IReadOnlyList<SpeechTranscriptionCandidate>>(),
            null,
            EmptyRecognitionContext,
            new AzureSpeechOptions(),
            NullLogger.Instance);

        transcription.IsAccepted.Should().BeFalse();
        transcription.Rejection.Should().Be(SpeechTranscriptionRejection.NoSpeech);
    }

    private static readonly SpeechRecognitionContext EmptyRecognitionContext =
        new(null, null, Array.Empty<string>(), Array.Empty<string>(), IncludeNameHints: false);

    [Fact]
    public void Wav_validation_rejects_compressed_or_wrong_format_audio()
    {
        WavAudio.TryExtractRequiredPcm(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, out _)
            .Should().BeFalse();
        WavAudio.TryExtractRequiredPcm(CreatePcmWav(44100, 1, new short[] { 1000 }), out _)
            .Should().BeFalse();
        WavAudio.TryExtractRequiredPcm(CreatePcmWav(16000, 2, new short[] { 1000, 1000 }), out _)
            .Should().BeFalse();
    }

    [Fact]
    public void Wav_validation_accepts_only_audible_16khz_mono_pcm()
    {
        WavAudio.TryExtractRequiredPcm(
            CreatePcmWav(16000, 1, new short[] { 0, 2000, -2000, 3000 }),
            out var pcm).Should().BeTrue();

        WavAudio.HasAudibleSignal(pcm).Should().BeTrue();
        WavAudio.HasAudibleSignal(new byte[3200]).Should().BeFalse();
        WavAudio.HasAudibleSignal(CreateQuietPcm()).Should().BeTrue();
    }

    [Fact]
    public void Wav_duration_uses_16khz_mono_pcm_byte_rate()
    {
        WavAudio.DurationSeconds(new byte[32_000]).Should().BeApproximately(1, 0.000001);
        WavAudio.DurationSeconds(Array.Empty<byte>()).Should().Be(0);
    }

    private static byte[] CreateQuietPcm()
    {
        var pcm = new byte[3200];
        for (var index = 0; index < pcm.Length; index += 2)
        {
            var sample = (short)(index % 4 == 0 ? 10 : -10);
            BitConverter.GetBytes(sample).CopyTo(pcm, index);
        }
        return pcm;
    }

    [Fact]
    public async Task Hermes_tutor_reports_unavailable_when_gateway_timeout_cancels_completion()
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.Model.Returns("slow-test");
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => WaitForCancellationAsync(call.Arg<CancellationToken>()));
        var tutor = new HermesConversationTutor(
            completion,
            NullLogger<HermesConversationTutor>.Instance,
            TimeSpan.FromMilliseconds(25));
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A1);
        session.AddLearnerTurn("I like apples.");

        var action = () => tutor.NextReplyAsync(session);

        await action.Should().ThrowAsync<SpeakingTutorUnavailableException>()
            .Where(exception => exception.Code == "timeout");
        await completion.Received(1).CompleteAsync(
            Arg.Any<string>(), Arg.Any<string>(), 96, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("timeout", "timeout")]
    [InlineData("provider_unavailable", "provider_unavailable")]
    [InlineData("queue_full", "provider_unavailable")]
    public async Task Hermes_tutor_translates_admission_failures_for_the_speaking_fallback(
        string admissionCode,
        string expectedSpeakingCode)
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.Model.Returns("admission-test");
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<string?>>(_ => throw new AiAdmissionException(
                admissionCode,
                "AI is temporarily unavailable.",
                5,
                admissionCode == "timeout" ? 504 : 503));
        var tutor = CreateTutor(completion);

        var action = () => tutor.NextReplyAsync(
            ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel"));

        await action.Should().ThrowAsync<SpeakingTutorUnavailableException>()
            .Where(exception => exception.Code == expectedSpeakingCode);
    }

    [Fact]
    public async Task Resilient_tutor_uses_local_reply_when_gateway_admission_rejects_the_turn()
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.Model.Returns("admission-test");
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<string?>>(_ => throw new AiAdmissionException(
                "queue_full",
                "AI queue is full.",
                5,
                503));
        var tutor = new ResilientConversationTutor(
            CreateTutor(completion),
            new LocalConversationTutor(),
            NullLogger<ResilientConversationTutor>.Instance);

        var reply = await tutor.NextReplyAsync(
            ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel"));

        reply.Should().NotBeNullOrWhiteSpace();
        reply.Should().Contain("travel", Exactly.Once());
    }

    [Fact]
    public async Task Hermes_tutor_does_not_replace_an_unavailable_ai_reply_with_local_text()
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.Model.Returns("empty-test");
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var tutor = CreateTutor(completion);
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel");

        var action = () => tutor.NextReplyAsync(session);

        await action.Should().ThrowAsync<SpeakingTutorUnavailableException>()
            .Where(exception => exception.Code == "provider_unavailable");
    }

    [Fact]
    public async Task Hermes_tutor_does_not_swallow_request_cancellation()
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.Model.Returns("cancel-test");
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled<string?>(call.Arg<CancellationToken>()));
        var tutor = CreateTutor(completion);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => tutor.NextReplyAsync(
            ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2),
            cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Resilient_tutor_uses_local_reply_when_gateway_response_is_unusable()
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.Model.Returns("unsafe-test");
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Привет");
        var tutor = new ResilientConversationTutor(
            CreateTutor(completion),
            new LocalConversationTutor(),
            NullLogger<ResilientConversationTutor>.Instance);

        var reply = await tutor.NextReplyAsync(
            ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel"));

        reply.Should().NotBeNullOrWhiteSpace();
        reply.Should().Contain("travel", Exactly.Once());
    }

    [Fact]
    public async Task Resilient_tutor_does_not_fallback_after_client_cancellation()
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.Model.Returns("cancel-test");
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled<string?>(call.Arg<CancellationToken>()));
        var tutor = new ResilientConversationTutor(
            CreateTutor(completion),
            new LocalConversationTutor(),
            NullLogger<ResilientConversationTutor>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => tutor.NextReplyAsync(
            ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2),
            cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class ResponseHandler(string payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            });
    }

    private sealed class CapturingResponseHandler(string payload) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public async Task Local_tutor_opens_on_the_chosen_topic()
    {
        var tutor = new LocalConversationTutor();
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel");

        var reply = await tutor.NextReplyAsync(session);

        reply.Should().Contain("travel");
    }

    [Fact]
    public async Task Local_tutor_falls_back_to_a_generic_greeting_without_a_topic()
    {
        var tutor = new LocalConversationTutor();
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);

        var reply = await tutor.NextReplyAsync(session);

        reply.Should().Contain("talk about today");
    }

    [Fact]
    public async Task Hermes_tutor_reports_unavailable_when_gateway_returns_no_text()
    {
        var completion = CreateHermesCompletion(
            """{"choices":[{"message":{"content":null},"finish_reason":"length"}]}""");
        var tutor = CreateTutor(completion);
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel");
        session.AddLearnerTurn("I like visiting new cities.");

        var action = () => tutor.NextReplyAsync(session);

        await action.Should().ThrowAsync<SpeakingTutorUnavailableException>()
            .Where(exception => exception.Code == "provider_unavailable");
    }

    [Fact]
    public async Task Local_tutor_answers_a_greeting_and_everyday_question_naturally()
    {
        var tutor = new LocalConversationTutor();
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        session.AddLearnerTurn("Hi, how are you? What are you doing?");

        var reply = await tutor.NextReplyAsync(session);

        reply.Should().Contain("doing well");
        reply.Should().Contain("practise English");
        reply.Should().NotContain("You said");
        reply.Should().NotContain("people, the places");
    }

    [Fact]
    public async Task Local_tutor_does_not_mistake_thing_for_the_greeting_hi()
    {
        var tutor = new LocalConversationTutor();
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "daily_life");
        session.AddLearnerTurn(
            "I have several, uh, daily routines, uh, for example, uh, first, uh, when I wake up in the morning, the first thing I do is check my phone because, uh, I may, uh, sleep, but a digital world, uh, wasn't.");

        var reply = await tutor.NextReplyAsync(session);

        reply.Should().NotStartWith("Hi!");
        reply.Should().Contain("morning routine");
        reply.Should().Contain("phone");
        reply.Count(character => character == '?').Should().Be(1);
    }

    [Theory]
    [InlineData("The first thing I do is drink water.")]
    [InlineData("This is part of my routine.")]
    [InlineData("I do something useful while I wait.")]
    public async Task Local_tutor_only_treats_hi_as_a_complete_greeting(string learnerAnswer)
    {
        var tutor = new LocalConversationTutor();
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        session.AddLearnerTurn(learnerAnswer);

        var reply = await tutor.NextReplyAsync(session);

        reply.Should().NotStartWith("Hi!");
    }

    [Theory]
    [InlineData("Hi")]
    [InlineData("Hello there")]
    [InlineData("Hey, can we practise?")]
    public async Task Local_tutor_still_recognizes_real_greetings(string learnerAnswer)
    {
        var tutor = new LocalConversationTutor();
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        session.AddLearnerTurn(learnerAnswer);

        var reply = await tutor.NextReplyAsync(session);

        reply.Should().StartWith("Hi!");
    }

    [Fact]
    public async Task Local_idea_cards_are_immediately_tied_to_the_topic_and_focus_word()
    {
        var generator = new LocalIdeaCardGenerator();
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, "daily_life", new[] { "routine" });

        var cards = await generator.GenerateAsync(session);

        cards.Should().HaveCount(4);
        cards.Should().Contain(card => card.Prompt.Contains("daily life"));
        cards.Should().Contain(card => card.Prompt.Contains("routine"));
    }

    [Fact]
    public async Task Llm_tutor_requests_a_substantive_answer_before_one_focused_question()
    {
        var handler = new CapturingResponseHandler(
            """{"choices":[{"message":{"content":"A longer useful answer. Which part should we explore next?"},"finish_reason":"stop"}]}""");
        var completion = CreateHermesCompletion(handler);
        var tutor = CreateTutor(completion);
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B2, "work");
        session.AddLearnerTurn("Can you explain how I can prepare for a software job interview?");

        await tutor.NextReplyAsync(session);

        handler.RequestBody.Should().NotBeNull();
        handler.RequestBody.Should().NotContain("1-3 short sentences");
        handler.RequestBody.Should().Contain("substantive");
        handler.RequestBody.Should().Contain("one focused follow-up question");
        handler.RequestBody.Should().Contain("transcript below");
        handler.RequestBody.Should().Contain("Learner: Can you explain how I can prepare");
    }

    [Fact]
    public async Task Llm_tutor_prompt_requires_direct_detail_grounded_non_repetitive_replies()
    {
        var handler = new CapturingResponseHandler(TutorReplyPayload);
        var tutor = CreateTutor(CreateHermesCompletion(handler));
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "my_mother");
        session.AddTutorTurn("That sounds interesting. What does she do in the morning?");
        session.AddLearnerTurn("She always wake up at five in the morning.");

        await tutor.NextReplyAsync(session);

        handler.RequestBody.Should().Contain("specific fact or detail from the learner");
        handler.RequestBody.Should().Contain("Do not repeat the opening phrase or question from recent Tutor turns");
        handler.RequestBody.Should().Contain("That sounds interesting");
        handler.RequestBody.Should().Contain("keep talking");
        handler.RequestBody.Should().Contain("Five is quite early");
        handler.RequestBody.Should().Contain("naturally recast");
        handler.RequestBody.Should().Contain("She always wakes up at five");
    }

    [Fact]
    public async Task Llm_tutor_prompt_treats_topic_as_context_not_a_forced_redirect()
    {
        var handler = new CapturingResponseHandler(TutorReplyPayload);
        var tutor = CreateTutor(CreateHermesCompletion(handler));
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "my_mother");
        session.AddLearnerTurn("What is your mission?");

        await tutor.NextReplyAsync(session);

        handler.RequestBody.Should().Contain("off-topic question, answer it directly");
        handler.RequestBody.Should().Contain("Do not force an immediate return");
        handler.RequestBody.Should().Contain("What is your mission?");
        handler.RequestBody.Should().NotContain("Stay on this topic at all times");
    }

    [Fact]
    public async Task Llm_tutor_sends_the_whole_transcript_while_the_session_is_short()
    {
        var handler = new CapturingResponseHandler(TutorReplyPayload);
        var completion = CreateHermesCompletion(handler);
        var tutor = CreateTutor(completion);
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1, "work");
        for (var i = 1; i <= 6; i++)
        {
            session.AddLearnerTurn($"Learner sentence number {i}.");
            session.AddTutorTurn($"Tutor sentence number {i}.");
        }

        await tutor.NextReplyAsync(session);

        // 12 turns is under the window, so nothing is dropped and no elision marker appears.
        for (var i = 1; i <= 6; i++)
            handler.RequestBody.Should().Contain($"Learner sentence number {i}.");
        handler.RequestBody.Should().NotContain("are omitted");
    }

    [Fact]
    public async Task Llm_tutor_elides_the_middle_of_a_long_transcript_but_keeps_the_opening_and_the_recent_turns()
    {
        var handler = new CapturingResponseHandler(TutorReplyPayload);
        var completion = CreateHermesCompletion(handler);
        var tutor = CreateTutor(completion);
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1, "work");
        for (var i = 1; i <= 25; i++)
        {
            session.AddLearnerTurn($"Learner sentence number {i}.");
            session.AddTutorTurn($"Tutor sentence number {i}.");
        }

        await tutor.NextReplyAsync(session);

        // The opening exchange survives - it carries setup the later turns assume.
        handler.RequestBody.Should().Contain("Learner sentence number 1.");
        handler.RequestBody.Should().Contain("Tutor sentence number 1.");

        // The most recent exchanges survive - the reply has to follow on from them.
        handler.RequestBody.Should().Contain("Learner sentence number 25.");
        handler.RequestBody.Should().Contain("Tutor sentence number 25.");

        // The middle is dropped, and the gap is marked so the model does not read it as a topic change.
        handler.RequestBody.Should().NotContain("Learner sentence number 10.");
        handler.RequestBody.Should().Contain("are omitted");
    }

    [Fact]
    public async Task Llm_tutor_input_stops_growing_once_the_transcript_window_is_full()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1, "work");
        for (var i = 1; i <= 20; i++)
        {
            session.AddLearnerTurn($"Learner sentence number {i}.");
            session.AddTutorTurn($"Tutor sentence number {i}.");
        }
        var atTwentyExchanges = await CapturePromptLengthAsync(session);

        for (var i = 21; i <= 60; i++)
        {
            session.AddLearnerTurn($"Learner sentence number {i}.");
            session.AddTutorTurn($"Tutor sentence number {i}.");
        }
        var atSixtyExchanges = await CapturePromptLengthAsync(session);

        // Tripling the session length must not grow the request: this is the whole point of the
        // window. A small delta is allowed for turn numbers widening from one digit to two.
        atSixtyExchanges.Should().BeCloseTo(atTwentyExchanges, 32);
    }

    private const string TutorReplyPayload =
        """{"choices":[{"message":{"content":"That is interesting. What happened next?"},"finish_reason":"stop"}]}""";

    private static HermesConversationTutor CreateTutor(ILlmCompletion completion) =>
        new(completion, NullLogger<HermesConversationTutor>.Instance);

    private static async Task<string?> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return null;
    }

    private static async Task<int> CapturePromptLengthAsync(ConversationSession session)
    {
        var handler = new CapturingResponseHandler(TutorReplyPayload);
        var tutor = CreateTutor(CreateHermesCompletion(handler));

        await tutor.NextReplyAsync(session);

        return handler.RequestBody!.Length;
    }

    [Fact]
    public async Task Hermes_tutor_uses_gateway_reply_when_it_is_valid()
    {
        var completion = CreateHermesCompletion(
            """{"choices":[{"message":{"content":"London sounds exciting. Would you visit in summer or winter?"},"finish_reason":"stop"}]}""");
        var tutor = CreateTutor(completion);
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, "travel");
        session.AddLearnerTurn("I want to visit London.");

        var reply = await tutor.NextReplyAsync(session);

        reply.Should().Be("London sounds exciting. Would you visit in summer or winter?");
    }

    [Fact]
    public void Feedback_provider_loads_templates_and_fills_placeholders()
    {
        var provider = JsonFeedbackTemplateProvider.FromEmbeddedResource();

        provider.Get("pron.good").Should().NotBeNullOrWhiteSpace();
        provider.Get("pron.mispronunciation", new Dictionary<string, string> { ["word"] = "three" })
            .Should().Contain("three").And.NotContain("{word}");
        provider.Get("does.not.exist").Should().BeNull();
    }

    [Fact]
    public void Phoneme_library_returns_word_phonetics_and_flags_hard_sounds()
    {
        var library = new PhonemeVisualLibrary();

        var three = library.GetWordPhonetics("Three");
        three.Should().NotBeNull();
        three!.Ipa.Should().Be("θriː");

        var th = library.GetByPhoneme("θ");
        th!.IsHardForUzbek.Should().BeTrue();
        th.TipCode.Should().Be("tip.th");

        library.GetWordPhonetics("unknownword").Should().BeNull();
    }

    [Fact]
    public void Phoneme_library_resolves_arbitrary_words_via_cmu_dictionary()
    {
        var library = new PhonemeVisualLibrary();

        // "listening" is not in the curated list but must still resolve (no 404 for real words).
        var listening = library.GetWordPhonetics("Listening");

        listening.Should().NotBeNull();
        listening!.Word.Should().Be("listening");
        listening.Ipa.Should().Contain("ɪ").And.Contain("ŋ");
        // L IH1 S AH0 N IH0 NG -> 7 phonemes, with AH0 mapped to schwa.
        listening.Phonemes.Should().Equal("l", "ɪ", "s", "ə", "n", "ɪ", "ŋ");
    }

    [Fact]
    public void Phoneme_library_returns_null_for_a_non_word()
    {
        var library = new PhonemeVisualLibrary();

        library.GetWordPhonetics("zzzqxw").Should().BeNull();
    }

    [Fact]
    public void Phoneme_library_resolves_multi_word_phrases()
    {
        var library = new PhonemeVisualLibrary();

        // Vocabulary teaches "bus stop" as one entry, but CMU only holds single words -
        // the phrase must still resolve (no "talaffuz topilmadi") by stitching its parts.
        var busStop = library.GetWordPhonetics("bus stop");

        busStop.Should().NotBeNull();
        busStop!.Word.Should().Be("bus stop");
        busStop.Ipa.Should().Contain(" "); // space-joined IPA of "bus" + "stop"
        var bus = library.GetWordPhonetics("bus")!;
        var stop = library.GetWordPhonetics("stop")!;
        busStop.Phonemes.Should().Equal(bus.Phonemes.Concat(stop.Phonemes));
    }

    [Fact]
    public void Phoneme_library_resolves_hyphenated_compounds()
    {
        var library = new PhonemeVisualLibrary();

        var wellKnown = library.GetWordPhonetics("well-known");

        wellKnown.Should().NotBeNull();
        wellKnown!.Word.Should().Be("well-known");
    }

    [Fact]
    public void Phoneme_library_returns_null_when_a_phrase_part_is_unknown()
    {
        var library = new PhonemeVisualLibrary();

        library.GetWordPhonetics("bus zzzqxw").Should().BeNull();
    }

    [Fact]
    public async Task Local_stt_never_invents_a_learner_transcript()
    {
        var stt = new LocalSpeechToTextService();

        var transcript = await stt.TranscribeAsync(new byte[] { 1, 2, 3 });

        transcript.IsAccepted.Should().BeFalse();
        transcript.Rejection.Should().Be(SpeechTranscriptionRejection.ServiceFailure);
    }

    private static byte[] CreatePcmWav(int sampleRate, short channels, IReadOnlyList<short> samples)
    {
        var dataSize = samples.Count * sizeof(short);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * sizeof(short));
        writer.Write((short)(channels * sizeof(short)));
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        foreach (var sample in samples)
            writer.Write(sample);
        return stream.ToArray();
    }

    [Fact]
    public async Task Local_tts_marks_fallback_audio_as_non_natural()
    {
        var tts = new LocalTextToSpeechService();

        var speech = await tts.SynthesizeAsync("Hello there");

        speech.AudioContent.Should().NotBeEmpty();
        speech.IsNaturalVoice.Should().BeFalse();
        speech.Timings.Select(timing => timing.Text).Should().Equal("Hello", "there");
        speech.Timings.Select(timing => timing.AudioOffset).Should().BeInAscendingOrder();
        speech.Visemes.Frames.Should().NotBeEmpty();
        speech.Visemes.Frames.Should()
            .OnlyContain(f => f.AudioOffset >= TimeSpan.Zero && f.AudioOffset <= speech.Visemes.AudioDuration);
        // The sequence carries a single renderable whole-utterance red-lips SVG (the same
        // single-inject-and-trigger contract Azure redlips_front uses), so the SVG pipeline is
        // exercisable without keys.
        speech.Visemes.Animation.Should().NotBeNull();
        speech.Visemes.Animation.Should().Contain("<svg").And.Contain("<animate");
    }

    [Fact]
    public async Task Local_assessor_penalizes_hard_sounds()
    {
        var assessor = new LocalPronunciationAssessor();

        var result = await assessor.AssessAsync(new byte[] { 1 }, "three the cat");

        // "three" contains both "th" and "r" -> should need practice.
        result.Words.Should().Contain(w => w.Word == "three" && w.NeedsPractice);
    }

    private static HermesGatewayLlmCompletion CreateHermesCompletion(string payload) =>
        CreateHermesCompletion(new ResponseHandler(payload));

    private static HermesGatewayLlmCompletion CreateHermesCompletion(HttpMessageHandler handler) =>
        new(
            new StubHttpClientFactory(handler),
            new HermesGatewayOptions
            {
                ApiKey = "test-key",
                Endpoint = "http://127.0.0.1:8642/v1",
                Model = "hermes-agent",
            },
            modelFallbacks: new[] { "hermes-agent" },
            maxAttemptsPerModel: 1,
            baseBackoffMs: 1);
}
