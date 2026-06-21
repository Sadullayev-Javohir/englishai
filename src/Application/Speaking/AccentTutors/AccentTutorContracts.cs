using Application.Speaking.Dtos;
using Application.Speaking;
using Application.Speaking.Ports;
using Application.Common;
using Domain.Speaking;
using MediatR;

namespace Application.Speaking.AccentTutors;

public sealed record AccentTutorMessage(string Role, string Text);

public sealed record AccentTutorStartQuery(string TutorId) : IRequest<AccentTutorStartResult>;
public sealed record AccentTutorNudgeQuery(
    string TutorId,
    IReadOnlyList<AccentTutorMessage>? History = null) : IRequest<AccentTutorStartResult>;

public sealed record AccentTutorStartResult(
    string Text,
    string TutorAudioBase64,
    bool IsNaturalVoice,
    IReadOnlyList<SpeechWordTimingDto> WordTimings);

public sealed record AccentTutorTurnCommand(
    string TutorId,
    byte[] AudioContent,
    IReadOnlyList<AccentTutorMessage>? History = null,
    bool IsInterruption = false) : IRequest<AccentTutorTurnResult>;

public sealed record AccentTutorAttemptScore(
    double OverallScore,
    double AccuracyScore,
    double FluencyScore,
    double CompletenessScore);
public sealed record AccentTutorEvaluateCommand(
    string TutorId,
    IReadOnlyList<AccentTutorMessage>? History,
    IReadOnlyList<AccentTutorAttemptScore>? Scores) : IRequest<AccentTutorEvaluationResult>;
public sealed record AccentTutorEvaluationResult(
    bool Evaluable,
    double OverallScore,
    double AccuracyScore,
    double FluencyScore,
    double CompletenessScore,
    string Feedback,
    string StrongestSkill,
    string FocusSkill);

public sealed record AccentTutorTurnResult(
    string Transcript,
    string TutorText,
    string TutorAudioBase64,
    bool IsNaturalVoice,
    PronunciationResultDto Pronunciation,
    IReadOnlyList<SpeechWordTimingDto> WordTimings);

public sealed record AccentTutorRecognizedEvent(string Transcript);
public sealed record AccentTutorReplyEvent(string TutorText);
public sealed record AccentTutorPronunciationEvent(PronunciationResultDto Pronunciation);
public sealed record AccentTutorAudioEvent(
    string TutorAudioBase64,
    bool IsNaturalVoice,
    IReadOnlyList<SpeechWordTimingDto> WordTimings);

public interface IAccentTutorAgent
{
    bool IsConfigured { get; }

    Task<string> ReplyAsync(
        string tutorId,
        IReadOnlyList<AccentTutorMessage> history,
        string learnerText,
        CancellationToken cancellationToken = default);
}

public interface IAccentTutorVoiceService
{
    Task<Models.SynthesizedSpeech> SynthesizeAsync(
        string tutorId,
        string text,
        CancellationToken cancellationToken = default);
}

public sealed class AccentTutorStartQueryHandler(
    IAccentTutorAgent agent,
    IAccentTutorVoiceService voice)
    : IRequestHandler<AccentTutorStartQuery, AccentTutorStartResult>
{
    public async Task<AccentTutorStartResult> Handle(
        AccentTutorStartQuery request,
        CancellationToken cancellationToken)
    {
        EnsureTutor(request.TutorId);
        if (!agent.IsConfigured)
            throw new SpeakingTutorUnavailableException(
                "accent_tutor_not_configured",
                retryable: false);

        var text = Clean(await agent.ReplyAsync(
            request.TutorId,
            Array.Empty<AccentTutorMessage>(),
            "Begin the live lesson now. Greet the learner briefly and ask one natural opening question.",
            cancellationToken));
        var speech = await voice.SynthesizeAsync(request.TutorId, text, cancellationToken);
        return new AccentTutorStartResult(
            text,
            Convert.ToBase64String(speech.AudioContent),
            speech.IsNaturalVoice,
            SpeechWordTimingDto.FromDomain(speech.Timings));
    }

    public static void EnsureTutor(string tutorId)
    {
        if (tutorId is not ("american" or "british" or "australian" or "irish"))
            throw new ArgumentException("Unknown accent tutor.", nameof(tutorId));
    }

    internal static string Clean(string text)
    {
        var clean = Common.SpeechText.Clean(text);
        return string.IsNullOrWhiteSpace(clean)
            ? "Hello! Let's practise natural English together. How are you today?"
            : clean;
    }
}

public sealed class AccentTutorNudgeQueryHandler(
    IAccentTutorAgent agent,
    IAccentTutorVoiceService voice)
    : IRequestHandler<AccentTutorNudgeQuery, AccentTutorStartResult>
{
    public async Task<AccentTutorStartResult> Handle(
        AccentTutorNudgeQuery request,
        CancellationToken cancellationToken)
    {
        AccentTutorStartQueryHandler.EnsureTutor(request.TutorId);
        if (!agent.IsConfigured)
            throw new SpeakingTutorUnavailableException(
                "accent_tutor_not_configured",
                retryable: false);

        var history = (request.History ?? Array.Empty<AccentTutorMessage>())
            .TakeLast(12)
            .ToArray();
        var text = AccentTutorStartQueryHandler.Clean(await agent.ReplyAsync(
            request.TutorId,
            history,
            "Re-engage the learner naturally with one short, friendly follow-up question. " +
            "Continue the current topic. Never mention silence, waiting, hearing problems, inactivity, or whether they are alive.",
            cancellationToken));
        var speech = await voice.SynthesizeAsync(request.TutorId, text, cancellationToken);
        return new AccentTutorStartResult(
            text,
            Convert.ToBase64String(speech.AudioContent),
            speech.IsNaturalVoice,
            SpeechWordTimingDto.FromDomain(speech.Timings));
    }
}

public sealed class AccentTutorTurnCommandHandler(
    IAccentTutorAgent agent,
    ISpeechToTextService speechToText,
    IPronunciationAssessor pronunciation,
    IAccentTutorVoiceService voice,
    ISpeakingPracticeWordRepository? practiceWords = null,
    ICurrentUserAccessor? currentUser = null,
    TimeProvider? clock = null)
    : IRequestHandler<AccentTutorTurnCommand, AccentTutorTurnResult>
{
    private const int MaxHistoryMessages = 16;
    private const int MaxHistoryTextLength = 2_000;

    public async Task<AccentTutorTurnResult> Handle(
        AccentTutorTurnCommand request,
        CancellationToken cancellationToken)
    {
        AccentTutorTurnResult? result = null;
        await ProcessAsync(request, (_, payload) =>
        {
            if (payload is AccentTutorTurnResult completed) result = completed;
            return Task.CompletedTask;
        }, cancellationToken);
        return result ?? throw new InvalidOperationException("Accent tutor turn did not complete.");
    }

    public async Task ProcessAsync(
        AccentTutorTurnCommand request,
        Func<string, object, Task> emit,
        CancellationToken cancellationToken)
    {
        AccentTutorStartQueryHandler.EnsureTutor(request.TutorId);
        if (!agent.IsConfigured)
            throw new SpeakingTutorUnavailableException(
                "accent_tutor_not_configured",
                retryable: false);
        if (request.AudioContent.Length == 0)
            throw new ArgumentException("Audio is required.", nameof(request));

        var recognitionContext = AccentTutorRecognitionContext.Create(request.TutorId, request.History);
        var transcription = speechToText is IContextualSpeechToTextService contextualSpeech
            ? await contextualSpeech.TranscribeAsync(request.AudioContent, recognitionContext, cancellationToken)
            : await speechToText.TranscribeAsync(request.AudioContent, cancellationToken);
        if (!transcription.IsAccepted)
            throw new InvalidOperationException("Speech could not be recognized. Please speak clearly and try again.");

        await ProcessRecognizedAsync(request, transcription, emit, cancellationToken);
    }

    public async Task ProcessRecognizedAsync(
        AccentTutorTurnCommand request,
        SpeechTranscription transcription,
        Func<string, object, Task> emit,
        CancellationToken cancellationToken)
    {
        AccentTutorStartQueryHandler.EnsureTutor(request.TutorId);
        if (!agent.IsConfigured)
            throw new SpeakingTutorUnavailableException(
                "accent_tutor_not_configured",
                retryable: false);
        if (!transcription.IsAccepted)
            throw new InvalidOperationException("Speech could not be recognized. Please speak clearly and try again.");

        var transcript = transcription.Text.Trim();
        if (request.IsInterruption && !InterruptionSpeechGate.IsAccepted(transcription))
            throw new InvalidOperationException("Interruption speech was not clear enough.");
        await emit("recognized", new AccentTutorRecognizedEvent(transcript));
        var safeHistory = (request.History ?? Array.Empty<AccentTutorMessage>())
            .TakeLast(MaxHistoryMessages)
            .Where(message => message.Role is "learner" or "tutor" && !string.IsNullOrWhiteSpace(message.Text))
            .Select(message => message with { Text = message.Text.Trim()[..Math.Min(message.Text.Trim().Length, MaxHistoryTextLength)] })
            .ToArray();

        var assessmentTask = pronunciation.AssessAsync(request.AudioContent, transcript, cancellationToken);
        var replyTask = agent.ReplyAsync(request.TutorId, safeHistory, transcript, cancellationToken);
        var reply = AccentTutorStartQueryHandler.Clean(await replyTask);
        await emit("tutor", new AccentTutorReplyEvent(reply));

        var speechTask = voice.SynthesizeAsync(request.TutorId, reply, cancellationToken);
        var assessment = await assessmentTask;
        await SavePracticeWordsAsync(assessment, cancellationToken);
        var pronunciationDto = PronunciationResultDto.FromDomain(assessment);
        await emit("pronunciation", new AccentTutorPronunciationEvent(pronunciationDto));

        var speech = await speechTask;
        var timings = SpeechWordTimingDto.FromDomain(speech.Timings);
        await emit("audio", new AccentTutorAudioEvent(
            Convert.ToBase64String(speech.AudioContent),
            speech.IsNaturalVoice,
            timings));

        await emit("completed", new AccentTutorTurnResult(
            transcript,
            reply,
            Convert.ToBase64String(speech.AudioContent),
            speech.IsNaturalVoice,
            pronunciationDto,
            timings));
    }

    private async Task SavePracticeWordsAsync(
        PronunciationResult result,
        CancellationToken cancellationToken)
    {
        if (!result.IsAuthentic || practiceWords is null || currentUser?.LearnerId is not { } learnerId)
            return;

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        foreach (var failed in result.WordsNeedingPractice
                     .GroupBy(word => SpeakingPracticeWord.Normalize(word.Word))
                     .Select(group => group.OrderBy(word => word.AccuracyScore).First()))
        {
            var normalized = SpeakingPracticeWord.Normalize(failed.Word);
            var word = await practiceWords.GetByLearnerAndWordAsync(learnerId, normalized, cancellationToken);
            if (word is null)
            {
                word = SpeakingPracticeWord.Create(
                    learnerId, failed.Word, failed.AccuracyScore, failed.ErrorType, now);
            }
            else
            {
                word.RecordFailure(failed.Word, failed.AccuracyScore, failed.ErrorType, now);
            }
            await practiceWords.SaveAsync(word, cancellationToken);
        }
    }
}

public sealed class AccentTutorEvaluateCommandHandler(IAccentTutorAgent agent)
    : IRequestHandler<AccentTutorEvaluateCommand, AccentTutorEvaluationResult>
{
    public async Task<AccentTutorEvaluationResult> Handle(
        AccentTutorEvaluateCommand request,
        CancellationToken cancellationToken)
    {
        AccentTutorStartQueryHandler.EnsureTutor(request.TutorId);
        var history = (request.History ?? Array.Empty<AccentTutorMessage>()).TakeLast(24).ToArray();
        var scores = request.Scores ?? Array.Empty<AccentTutorAttemptScore>();
        if (history.All(message => message.Role != "learner") || scores.Count == 0)
            return new(false, 0, 0, 0, 0, "Avval ingliz tilida bir necha gap ayting, keyin AI sizga aniq fikr beradi.", "—", "—");

        var overall = scores.Average(score => score.OverallScore);
        var dimensions = new Dictionary<string, double>
        {
            ["Accuracy"] = scores.Average(score => score.AccuracyScore),
            ["Fluency"] = scores.Average(score => score.FluencyScore),
            ["Completeness"] = scores.Average(score => score.CompletenessScore),
        };
        var strongest = dimensions.MaxBy(pair => pair.Value).Key;
        var focus = dimensions.MinBy(pair => pair.Value).Key;
        var feedback = await agent.ReplyAsync(
            request.TutorId,
            history,
            $"The live lesson has ended. Give the learner concise personalized feedback in two short English sentences. " +
            $"Overall {overall:F0}/100, accuracy {dimensions["Accuracy"]:F0}, fluency {dimensions["Fluency"]:F0}, " +
            $"completeness {dimensions["Completeness"]:F0}. Praise their {strongest.ToLowerInvariant()} and give one practical tip for {focus.ToLowerInvariant()}. " +
            "Do not ask a question and do not use markdown.",
            cancellationToken);

        return new(
            true,
            Math.Round(overall, 1),
            Math.Round(dimensions["Accuracy"], 1),
            Math.Round(dimensions["Fluency"], 1),
            Math.Round(dimensions["Completeness"], 1),
            AccentTutorStartQueryHandler.Clean(feedback),
            strongest,
            focus);
    }
}

public static class InterruptionSpeechGate
{
    public static bool IsAccepted(SpeechTranscription transcription)
    {
        if (!transcription.IsAccepted || transcription.RequiresConfirmation) return false;
        var words = transcription.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return false;
        var confidence = transcription.Confidence ?? 0;
        // A clear command such as “stop” is allowed; other barge-ins need two words and stronger
        // recognition so music/TV lyrics and room noise do not cut tutor playback.
        var command = words.Length <= 3 && words.Any(word => word.Trim('.', ',', '!', '?').Equals("stop", StringComparison.OrdinalIgnoreCase));
        return command ? confidence >= 0.72 : words.Length >= 2 && confidence >= 0.78;
    }
}

public static class AccentTutorRecognitionContext
{
    private static readonly string[] LevelPhrases =
    {
        "My English level is A1", "My English level is A2", "My English level is B1",
        "My English level is B2", "My English level is C1", "My English level is C2",
        "English level", "beginner level", "elementary level", "intermediate level",
        "upper intermediate level", "advanced level", "A one", "A two", "B one", "B two", "C one", "C two",
    };

    public static SpeechRecognitionContext Create(
        string tutorId,
        IReadOnlyList<AccentTutorMessage>? history)
    {
        var recent = (history ?? Array.Empty<AccentTutorMessage>()).TakeLast(12).ToArray();
        var lastTutor = recent.LastOrDefault(message => message.Role == "tutor")?.Text;
        var context = recent
            .Select(message => message.Text)
            .Concat(LevelPhrases)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new SpeechRecognitionContext(
            lastTutor,
            "English speaking lesson and CEFR level",
            LevelPhrases,
            context,
            IncludeNameHints: false,
            RecognitionLanguage: tutorId switch
            {
                "british" => "en-GB",
                "australian" => "en-AU",
                "irish" => "en-IE",
                _ => "en-US",
            });
    }
}
