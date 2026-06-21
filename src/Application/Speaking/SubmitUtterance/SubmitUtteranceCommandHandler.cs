using Application.Ai;
using Application.Analytics.Ports;
using Application.Common;
using Application.Gamification;
using Application.Identity.Ports;
using Application.Speaking.Common;
using Application.Speaking.Dtos;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Subscription.Entitlements;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Analytics;
using Domain.Learning;
using Domain.Speaking;
using Domain.Vocabulary;
using MediatR;

namespace Application.Speaking.SubmitUtterance;

public sealed record RecognizedUtteranceEvent(string Text);

public sealed record TutorUtteranceEvent(
    string Text,
    bool SessionLimitReached,
    bool NamePrompt,
    TopicSpeakingProgressDto? TopicProgress,
    TopicCompletionDto? Completion,
    SpeakingQuotaDto? Quota = null);

/// <summary>
/// What is left of the learner's daily speaking budget, sent with each tutor reply so the client can
/// show it counting down instead of the learner discovering the limit only when they hit it.
/// </summary>
public sealed record SpeakingQuotaDto(double LimitMinutes, double UsedMinutes, double RemainingMinutes)
{
    public static SpeakingQuotaDto FromDomain(Domain.Subscription.SpeakingMinuteDecision decision) =>
        new(
            Math.Round(decision.LimitMinutes, 2),
            Math.Round(decision.UsedMinutes, 2),
            Math.Round(decision.RemainingMinutes, 2));
}

public sealed record PronunciationUtteranceEvent(
    PronunciationResultDto Pronunciation,
    string? FeedbackUz,
    string? FocusWord);

public sealed record AudioUtteranceEvent(
    string AudioBase64,
    IReadOnlyList<VisemeFrameDto> Visemes,
    string? VisemeAnimation,
    bool IsNaturalVoice,
    IReadOnlyList<SpeechWordTimingDto> WordTimings);

public sealed record UnrecognizedUtteranceEvent(string? FeedbackUz, string RejectionCode);

public sealed record TranscriptConfirmationRequiredEvent(
    string SuggestedText,
    IReadOnlyList<TranscriptAlternativeDto> Alternatives);

public sealed class SubmitUtteranceCommandHandler
    : IRequestHandler<SubmitUtteranceCommand, SubmitUtteranceResult>
{
    private readonly IConversationStore _conversations;
    private readonly ISpeechToTextService _stt;
    private readonly IPronunciationAssessor _assessor;
    private readonly IConversationTutor _tutor;
    private readonly ITextToSpeechService _tts;
    private readonly IFeedbackTemplateProvider _feedback;
    private readonly ITopicSpeakingProgressStore _topicProgress;
    private readonly IVocabularyTopicRepository _topics;
    private readonly ITopicCompletionStore _completions;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly IUserAccountStore _accounts;
    private readonly IProductEventStore _productEvents;
    private readonly ISpeakingPracticeWordRepository _practiceWords;
    private readonly IPhonemeVisualLibrary? _phonemeLibrary;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;
    private readonly IAiFeatureScope _aiScope;
    private readonly ISpeakingCurriculumProvider _curriculum;
    private readonly ISpeakingMinuteAllowanceService? _speakingMinutes;

    public SubmitUtteranceCommandHandler(
        IConversationStore conversations,
        ISpeechToTextService stt,
        IPronunciationAssessor assessor,
        IConversationTutor tutor,
        ITextToSpeechService tts,
        IFeedbackTemplateProvider feedback,
        ITopicSpeakingProgressStore topicProgress,
        IVocabularyTopicRepository topics,
        ITopicCompletionStore completions,
        IDailyProgressRecorder dailyProgress,
        IUserAccountStore accounts,
        IProductEventStore productEvents,
        TimeProvider clock,
        ISpeakingPracticeWordRepository? practiceWords = null,
        IPhonemeVisualLibrary? phonemeLibrary = null,
        ICurrentUserAccessor? currentUser = null,
        IAiFeatureScope? aiScope = null,
        ISpeakingCurriculumProvider? curriculum = null,
        // Optional so unit tests can build the handler without the subscription stack; DI always
        // supplies it. When absent the pipeline runs unmetered, which is only ever the test path.
        ISpeakingMinuteAllowanceService? speakingMinutes = null)
    {
        _speakingMinutes = speakingMinutes;
        _conversations = conversations;
        _stt = stt;
        _assessor = assessor;
        _tutor = tutor;
        _tts = tts;
        _feedback = feedback;
        _topicProgress = topicProgress;
        _topics = topics;
        _completions = completions;
        _dailyProgress = dailyProgress;
        _accounts = accounts;
        _productEvents = productEvents;
        _practiceWords = practiceWords ?? new NullSpeakingPracticeWordRepository();
        _phonemeLibrary = phonemeLibrary;
        _clock = clock;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
        _currentUser = currentUser;
        _curriculum = curriculum ?? new NullCurriculumProvider();
    }

    public async Task<SubmitUtteranceResult> Handle(
        SubmitUtteranceCommand request,
        CancellationToken cancellationToken)
    {
        RecognizedUtteranceEvent? recognized = null;
        TutorUtteranceEvent? tutor = null;
        PronunciationUtteranceEvent? pronunciation = null;
        AudioUtteranceEvent? audio = null;
        UnrecognizedUtteranceEvent? unrecognized = null;
        TranscriptConfirmationRequiredEvent? confirmation = null;

        await ProcessAsync(request, async (eventName, payload) =>
        {
            switch (eventName)
            {
                case "recognized": recognized = (RecognizedUtteranceEvent)payload; break;
                case "tutor": tutor = (TutorUtteranceEvent)payload; break;
                case "pronunciation": pronunciation = (PronunciationUtteranceEvent)payload; break;
                case "audio": audio = (AudioUtteranceEvent)payload; break;
                case "unrecognized": unrecognized = (UnrecognizedUtteranceEvent)payload; break;
                case "transcript-confirmation-required":
                    confirmation = (TranscriptConfirmationRequiredEvent)payload;
                    break;
                case "progress":
                    var progress = (TutorUtteranceEvent)payload;
                    tutor = tutor is null
                        ? progress
                        : tutor with
                        {
                            TopicProgress = progress.TopicProgress ?? tutor.TopicProgress,
                            Completion = progress.Completion ?? tutor.Completion,
                        };
                    break;
            }
            await Task.CompletedTask;
        }, cancellationToken);

        if (unrecognized is not null || recognized is null)
        {
            return new SubmitUtteranceResult(
                false, string.Empty, null, string.Empty, string.Empty,
                Array.Empty<VisemeFrameDto>(), null,
                unrecognized?.FeedbackUz, null,
                SessionLimitReached: tutor?.SessionLimitReached ?? false,
                WordTimings: Array.Empty<SpeechWordTimingDto>(),
                RejectionCode: unrecognized?.RejectionCode,
                RequiresTranscriptConfirmation: confirmation is not null,
                TranscriptAlternatives: confirmation?.Alternatives ?? Array.Empty<TranscriptAlternativeDto>());
        }

        return new SubmitUtteranceResult(
            true,
            recognized.Text,
            pronunciation?.Pronunciation,
            tutor?.Text ?? string.Empty,
            audio?.AudioBase64 ?? string.Empty,
            audio?.Visemes ?? Array.Empty<VisemeFrameDto>(),
            audio?.VisemeAnimation,
            pronunciation?.FeedbackUz,
            pronunciation?.FocusWord,
            tutor?.TopicProgress,
            tutor?.Completion,
            tutor?.SessionLimitReached ?? false,
            tutor?.NamePrompt ?? false,
            audio?.IsNaturalVoice ?? false,
            audio?.WordTimings ?? Array.Empty<SpeechWordTimingDto>());
    }

    public async Task ProcessAsync(
        SubmitUtteranceCommand request,
        Func<string, object, Task> emit,
        CancellationToken cancellationToken)
    {
        var session = await _conversations.GetAsync(request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ConversationSession), request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);

        var account = await _accounts.GetByIdAsync(session.LearnerId, cancellationToken);
        if (account is not null && account.PreferredName != session.LearnerName)
            session.SetLearnerName(account.PreferredName);

        if (session.HasReachedSpeakingLimit)
        {
            await emit("tutor", new TutorUtteranceEvent(
                string.Empty, true, false, null, null));
            return;
        }

        // Before the audio reaches speech-to-text: the whole point of the budget is not to pay for
        // the transcription. A confirmed transcript skips STT, so it costs nothing and is not gated.
        var confirmedTranscript = NormalizeConfirmedTranscript(request.ConfirmedTranscript);
        if (_speakingMinutes is not null && confirmedTranscript is null)
            await _speakingMinutes.EnsureAllowedAsync(session.LearnerId, cancellationToken);

        SpeechTranscription? transcription = null;
        if (confirmedTranscript is null)
        {
            transcription = _stt is IContextualSpeechToTextService contextualStt
                ? await contextualStt.TranscribeAsync(
                    request.AudioContent,
                    SpeechRecognitionContextFactory.FromSession(session),
                    cancellationToken)
                : await _stt.TranscribeAsync(request.AudioContent, cancellationToken);
            if (!transcription.IsAccepted)
            {
                await emit("unrecognized", new UnrecognizedUtteranceEvent(
                    _feedback.Get("pron.not_recognized"),
                    RejectionCode(transcription.Rejection)));
                return;
            }
            if (transcription.RequiresConfirmation)
            {
                var alternatives = TranscriptAlternatives(transcription);
                SpeakingRecognitionTelemetry.Record(
                    "confirmation_required",
                    transcription.Confidence,
                    transcription.ScoreGap);
                await emit(
                    "transcript-confirmation-required",
                    new TranscriptConfirmationRequiredEvent(transcription.Text, alternatives));
                return;
            }

            SpeakingRecognitionTelemetry.Record(
                "automatic",
                transcription.Confidence,
                transcription.ScoreGap);
        }
        else
        {
            SpeakingRecognitionTelemetry.Record(request.ConfirmationOutcome ?? "edited");
        }
        var recognizedText = confirmedTranscript ?? transcription!.Text;

        await emit("recognized", new RecognizedUtteranceEvent(recognizedText));

        // Booked only once the transcript is accepted, and only for audio that actually went to
        // speech-to-text. A rejected or unintelligible clip is not the learner's fault and Azure
        // charged us regardless, but taking their allowance for it would punish the wrong person.
        SpeakingQuotaDto? quota = null;
        if (_speakingMinutes is not null && confirmedTranscript is null)
        {
            quota = SpeakingQuotaDto.FromDomain(await _speakingMinutes.RecordAsync(
                session.LearnerId,
                SpokenAudioDuration.Minutes(request.AudioContent),
                cancellationToken));
        }

        var firstLearnerTurnInSession = session.LearnerTurnCount == 0;
        // A tutor timeout happens after recognition and after the learner turn is attached to the
        // in-memory session. Retrying the saved audio must continue that same turn, not append it again.
        var retryingPendingLearnerTurn = session.HasPendingLearnerTurn
            && string.Equals(session.LastTurn!.Text, recognizedText, StringComparison.Ordinal);
        if (!retryingPendingLearnerTurn)
        {
            session.AddLearnerTurn(recognizedText);
            // Persist recognition before the remote tutor call. If Hermes times out, the retry can
            // identify and continue this pending turn instead of creating a duplicate learner turn.
            await _conversations.SaveAsync(session, cancellationToken);
        }
        if (session.VocabularyTopicId is null
            && session.ScenarioCode is null
            && string.IsNullOrWhiteSpace(session.Topic)
            && session.CurriculumContext is null)
        {
            var dynamicContext = await _curriculum.MatchAsync(session.Level, recognizedText, cancellationToken);
            if (dynamicContext is not null) session.SetCurriculumContext(dynamicContext);
        }
        var spokenReference = EnglishSpokenForm.NormalizeText(recognizedText);
        // Sampled, not every turn: assessment re-bills the same audio speech-to-text already charged
        // for (docs/development-guide.md rule 10). See ConversationSession.ShouldAssessPronunciation.
        var pronunciationTask = session.ShouldAssessPronunciation
            ? AssessPronunciationAsync(request.AudioContent, spokenReference, cancellationToken)
            : Task.FromResult<PronunciationResult?>(null);
        using var aiScope = await _aiScope.EnterAsync(AiFeature.SpeakingTutor, session.LearnerId, cancellationToken);
        var tutorReplyTask = _tutor.NextReplyAsync(session, cancellationToken);

        var now = _clock.GetUtcNow();
        var spokenDelta = session.RecordSpokenActivity(now);

        var rawReply = await tutorReplyTask;
        var tutorReply = SpeechText.Clean(rawReply);
        if (string.IsNullOrWhiteSpace(tutorReply))
            tutorReply = "Could you tell me a little more about that?";
        var topicProgress = await UpdateTopicProgressAsync(session, spokenDelta, now, cancellationToken);
        // Best-effort from here: the learner has already spoken, the turn has already been
        // transcribed, assessed and answered (all of it paid for), so a gamification or analytics
        // blip must not turn that into a failed turn they are asked to repeat.
        await LearningRewards.AwardBestEffortAsync(
            () => _dailyProgress.RecordSkillAsync(
                session.LearnerId, SkillType.Speaking, 75, now, cancellationToken),
            cancellationToken);
        if (firstLearnerTurnInSession)
        {
            await LearningRewards.AwardBestEffortAsync(
                () => _productEvents.AppendOnceAsync(
                    session.LearnerId,
                    ProductEventType.SpeakingSessionCompleted,
                    now,
                    source: session.Id.ToString(),
                    cancellationToken),
                cancellationToken);
        }

        var namePrompt = string.IsNullOrWhiteSpace(session.LearnerName)
            && SpeakingIntent.LooksLikeNameIntroduction(recognizedText);
        await emit("tutor", new TutorUtteranceEvent(
            tutorReply,
            session.HasReachedSpeakingLimit,
            namePrompt,
            topicProgress,
            null,
            quota));

        var speechTask = _tts.SynthesizeAsync(tutorReply, cancellationToken);
        var pronunciation = await pronunciationTask;
        if (pronunciation is not null)
            session.AttachPronunciationToLastLearnerTurn(pronunciation);
        session.InsertTutorTurnAfterLastLearner(tutorReply);
        if (pronunciation is not null)
        {
            // The drill queue is an enrichment: the same word resurfaces on the next mispronunciation,
            // so losing one harvest is far cheaper than losing the turn.
            await LearningRewards.AwardBestEffortAsync(
                () => RecordPracticeWordsAsync(session.LearnerId, pronunciation, now, cancellationToken),
                cancellationToken);
        }
        var completion = await CreditTopicCompletionAsync(
            session, topicProgress, now, cancellationToken);
        // One commit for every database change this turn made (spoken-time progress and the topic
        // completion record). Committing them separately would let a failure credit a learner's
        // speaking time without the module score it earned, or the reverse.
        await _completions.CommitAsync(cancellationToken);
        await _conversations.SaveAsync(session, cancellationToken);

        if (pronunciation is not null)
        {
            var focusWord = pronunciation.WordsNeedingPractice.FirstOrDefault();
            var feedbackUz = _feedback.Get(
                FeedbackCode(focusWord),
                focusWord is null ? null : new Dictionary<string, string> { ["word"] = focusWord.Word });
            await emit("pronunciation", new PronunciationUtteranceEvent(
                PronunciationResultDto.FromDomain(pronunciation),
                feedbackUz,
                focusWord?.Word));
        }

        if (completion is not null)
        {
            await emit("progress", new TutorUtteranceEvent(
                tutorReply,
                session.HasReachedSpeakingLimit,
                namePrompt,
                topicProgress,
                completion,
                quota));
        }

        var speech = await speechTask;
        await emit("audio", new AudioUtteranceEvent(
            Convert.ToBase64String(speech.AudioContent),
            VisemeFrameDto.FromDomain(speech.Visemes),
            speech.Visemes.Animation,
            speech.IsNaturalVoice,
            SpeechWordTimingDto.FromDomain(speech.Timings)));
    }

    private sealed class NullCurriculumProvider : ISpeakingCurriculumProvider
    {
        public Task<SpeakingCurriculumContext?> ForTopicAsync(Guid topicId, CancellationToken cancellationToken) => Task.FromResult<SpeakingCurriculumContext?>(null);
        public Task<SpeakingCurriculumContext?> MatchAsync(Domain.Assessment.CefrLevel level, string text, CancellationToken cancellationToken) => Task.FromResult<SpeakingCurriculumContext?>(null);
    }

    private async Task<TopicCompletionDto?> CreditTopicCompletionAsync(
        ConversationSession session, TopicSpeakingProgressDto? topicProgress, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (session.VocabularyTopicId is not { } topicId)
            return null;

        var topic = await _topics.GetByIdAsync(topicId, cancellationToken);
        if (topic is null)
            return null;

        var record = await _completions.GetAsync(session.LearnerId, topicId, cancellationToken)
                     ?? TopicCompletionRecord.Start(session.LearnerId, topicId, topic.Level, now);

        if (topicProgress?.Learned == true)
        {
            // Pronunciation is sampled, not scored on every turn, and an assessment can also fail.
            // Averaging over an empty set would book a fabricated zero for a learner who spoke well
            // (docs/development-guide.md rule 8), so an unscored sitting records no speaking score at all - the
            // topic simply stays open until a sitting that did produce one.
            var assessedScores = session.Turns
                .Where(turn => turn.Role == ConversationRole.Learner && turn.Pronunciation is not null)
                .Select(turn => turn.Pronunciation!.OverallScore)
                .ToList();
            var progress = assessedScores.Count == 0
                ? null
                : await _topicProgress.GetAsync(session.LearnerId, topicId, cancellationToken);
            if (progress is not null)
            {
                var assessedScore = (int)Math.Round(assessedScores.Average());
                progress.RecordSession(session.Id, now, assessedScore);
                // Staged, not committed: the speaking progress and the completion record it feeds
                // must land together, or a learner is credited on one row and not the other.
                await _topicProgress.TrackAsync(progress, cancellationToken);
                record.RecordModule(SkillType.Speaking, assessedScore, now);
            }
        }

        await _completions.TrackAsync(record, cancellationToken);
        return TopicCompletionDto.FromDomain(record);
    }

    private async Task<TopicSpeakingProgressDto?> UpdateTopicProgressAsync(
        ConversationSession session, TimeSpan spokenDelta, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (session.VocabularyTopicId is not { } topicId)
            return null;

        var progress = await _topicProgress.GetAsync(session.LearnerId, topicId, cancellationToken);
        var justLearned = false;
        if (spokenDelta > TimeSpan.Zero)
        {
            progress ??= TopicSpeakingProgress.Start(session.LearnerId, topicId, now);
            justLearned = progress.AddSpeaking(spokenDelta, now);
            // Staged; the turn commits every database change once, below.
            await _topicProgress.TrackAsync(progress, cancellationToken);
        }

        var spokenSeconds = (int)Math.Round((progress?.SpokenTime ?? TimeSpan.Zero).TotalSeconds);
        return new TopicSpeakingProgressDto(
            spokenSeconds,
            (int)TopicSpeakingProgress.RequiredSpeakingTime.TotalSeconds,
            progress?.IsLearned ?? false,
            justLearned);
    }

    private static string FeedbackCode(WordPronunciation? focusWord) => focusWord?.ErrorType switch
    {
        null => "pron.good",
        PronunciationErrorType.Mispronunciation => "pron.mispronunciation",
        PronunciationErrorType.Omission => "pron.omission",
        PronunciationErrorType.Insertion => "pron.insertion",
        _ => "pron.low_accuracy"
    };

    private static string RejectionCode(SpeechTranscriptionRejection rejection) => rejection switch
    {
        SpeechTranscriptionRejection.InvalidAudio => "invalid_audio",
        SpeechTranscriptionRejection.LowConfidence => "low_confidence",
        SpeechTranscriptionRejection.ServiceFailure => "service_failure",
        _ => "no_speech",
    };

    private static string? NormalizeConfirmedTranscript(string? value)
    {
        if (value is null) return null;
        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length is 0 or > 1000)
            throw new ArgumentException("Confirmed transcript is invalid.");
        return normalized;
    }

    private static IReadOnlyList<TranscriptAlternativeDto> TranscriptAlternatives(
        SpeechTranscription transcription)
    {
        var alternatives = transcription.Candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Text))
            .Select(candidate => new TranscriptAlternativeDto(
                candidate.Text.Trim(),
                Math.Round(candidate.Confidence, 3)))
            .DistinctBy(candidate => candidate.Text, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        if (alternatives.All(candidate =>
                !string.Equals(candidate.Text, transcription.Text, StringComparison.OrdinalIgnoreCase)))
        {
            alternatives.Insert(0, new TranscriptAlternativeDto(
                transcription.Text,
                Math.Round(transcription.Confidence ?? 0, 3)));
        }
        return alternatives.Take(3).ToArray();
    }

    private async Task RecordPracticeWordsAsync(
        Guid learnerId,
        PronunciationResult pronunciation,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!pronunciation.IsAuthentic)
            return;

        foreach (var failedWord in pronunciation.WordsNeedingPractice
                     .GroupBy(word => SpeakingPracticeWord.Normalize(word.Word))
                     .Select(group => group.OrderBy(word => word.AccuracyScore).First()))
        {
            if (_phonemeLibrary is not null
                && !PronunciationDetailResolver.IsSupported(_phonemeLibrary, failedWord.Word))
            {
                continue;
            }

            var normalizedWord = SpeakingPracticeWord.Normalize(failedWord.Word);
            var practiceWord = await _practiceWords.GetByLearnerAndWordAsync(
                learnerId, normalizedWord, cancellationToken);

            if (practiceWord is null)
            {
                practiceWord = SpeakingPracticeWord.Create(
                    learnerId,
                    failedWord.Word,
                    failedWord.AccuracyScore,
                    failedWord.ErrorType,
                    now);
            }
            else
            {
                practiceWord.RecordFailure(
                    failedWord.Word,
                    failedWord.AccuracyScore,
                    failedWord.ErrorType,
                    now);
            }

            await _practiceWords.SaveAsync(practiceWord, cancellationToken);
        }
    }

    private sealed class NullSpeakingPracticeWordRepository : ISpeakingPracticeWordRepository
    {
        public Task<SpeakingPracticeWord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpeakingPracticeWord?>(null);

        public Task<SpeakingPracticeWord?> GetByLearnerAndWordAsync(
            Guid learnerId, string normalizedWord, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpeakingPracticeWord?>(null);

        public Task<IReadOnlyList<SpeakingPracticeWord>> GetActiveAsync(
            Guid learnerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SpeakingPracticeWord>>(Array.Empty<SpeakingPracticeWord>());

        public Task SaveAsync(SpeakingPracticeWord word, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private async Task<PronunciationResult?> AssessPronunciationAsync(
        byte[] audioContent,
        SpokenFormText reference,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _assessor.AssessAsync(audioContent, reference.Spoken, cancellationToken);
            return reference.MapResult(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Pronunciation feedback is an optional enrichment. A malformed or temporarily
            // unavailable Azure assessment must not discard the already recognized transcript or
            // turn a valid AI tutor reply into an SSE error. Do not fabricate a score; simply omit
            // pronunciation feedback for this turn and let the conversation continue.
            return null;
        }
    }
}
