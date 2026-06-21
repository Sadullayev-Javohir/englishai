using System.Collections.Concurrent;
using System.Diagnostics;
using Application.Common;
using Application.Speaking.Ports;
using Application.Speaking.SubmitUtterance;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Web.Observability;

namespace Web.Hubs;

public sealed class SpeakingLiveHub : Hub
{
    public const string SessionReadyEvent = "SessionReady";
    public const string PartialTranscriptEvent = "PartialTranscript";
    public const string FinalTranscriptEvent = "FinalTranscript";
    public const string TutorTextEvent = "TutorText";
    public const string PronunciationReadyEvent = "PronunciationReady";
    public const string TutorAudioEvent = "TutorAudio";
    public const string UnrecognizedEvent = "Unrecognized";
    public const string TranscriptConfirmationRequiredEvent = "TranscriptConfirmationRequired";
    public const string TurnCompletedEvent = "TurnCompleted";
    public const string RecoverableErrorEvent = "RecoverableError";

    private static readonly ConcurrentDictionary<string, ConnectionState> Connections = new();

    private readonly SubmitUtteranceCommandHandler _utterances;
    private readonly IConversationStore _conversations;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly SpeakingLiveOptions _options;
    private readonly SpeakingLiveTurnRegistry _turnRegistry;
    private readonly TimeProvider _clock;
    private readonly ILogger<SpeakingLiveHub> _logger;

    public SpeakingLiveHub(
        SubmitUtteranceCommandHandler utterances,
        IConversationStore conversations,
        ICurrentUserAccessor currentUser,
        IOptions<SpeakingLiveOptions> options,
        SpeakingLiveTurnRegistry turnRegistry,
        TimeProvider clock,
        ILogger<SpeakingLiveHub> logger)
    {
        _utterances = utterances;
        _conversations = conversations;
        _currentUser = currentUser;
        _options = options.Value;
        _turnRegistry = turnRegistry;
        _clock = clock;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        EnglishAiTelemetry.SignalRConnections.Add(
            1,
            new KeyValuePair<string, object?>("hub", "speaking-live"));
        EnglishAiTelemetry.SignalRConnectionEvents.Add(1, new("hub", "speaking-live"), new("outcome", "connected"));
        Connections.TryAdd(Context.ConnectionId, new ConnectionState());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var state))
            state.Dispose();

        EnglishAiTelemetry.SignalRConnections.Add(
            -1,
            new KeyValuePair<string, object?>("hub", "speaking-live"));
        EnglishAiTelemetry.SignalRConnectionEvents.Add(
            1,
            new("hub", "speaking-live"),
            new("outcome", exception is null ? "disconnected" : "failed"));
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<SpeakingLiveSessionReady> JoinSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new HubException("Live speaking is disabled.");

        var session = await _conversations.GetAsync(sessionId, cancellationToken)
            ?? throw new HubException("Speaking session was not found.");
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);

        var state = State;
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            state.SessionId = sessionId;
        }
        finally
        {
            state.Gate.Release();
        }

        return new SpeakingLiveSessionReady(sessionId, true, _clock.GetUtcNow());
    }

    public async Task SubmitTurn(
        SpeakingLiveTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new HubException("Live speaking is disabled.");
        if (string.IsNullOrWhiteSpace(request.AudioBase64))
            throw new HubException("Audio is required.");
        if (request.ConfirmedTranscript is not null && string.IsNullOrWhiteSpace(request.ConfirmedTranscript))
            throw new HubException("Confirmed transcript is invalid.");
        if (request.ConfirmationOutcome is not null
            && request.ConfirmationOutcome is not ("candidate_selected" or "edited"))
            throw new HubException("Confirmation outcome is invalid.");

        byte[] audio;
        try
        {
            audio = Convert.FromBase64String(request.AudioBase64);
        }
        catch (FormatException)
        {
            throw new HubException("Audio payload is invalid.");
        }

        if (audio.Length == 0 || audio.Length > _options.MaxAudioBytes)
            throw new HubException("Audio payload size is invalid.");

        var state = State;
        CancellationTokenSource turnCancellation;

        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            if (state.SessionId != request.SessionId)
                throw new HubException("Join the speaking session before submitting audio.");
            var turnClaim = _turnRegistry.Claim(request.TurnId, _options.ProcessedTurnCacheSize);
            if (turnClaim == TurnClaim.Completed)
            {
                await Clients.Caller.SendAsync(
                    TurnCompletedEvent,
                    new SpeakingLiveTurnCompleted(
                        request.SessionId,
                        request.TurnId,
                        request.Sequence,
                        _clock.GetUtcNow()),
                    cancellationToken);
                return;
            }
            if (turnClaim == TurnClaim.Processing)
                throw new HubException("This live speaking turn is already processing.");

            state.ActiveTurn?.Cancel();
            turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                Context.ConnectionAborted);
            state.ActiveTurn = turnCancellation;
        }
        finally
        {
            state.Gate.Release();
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await _utterances.ProcessAsync(
                new SubmitUtteranceCommand(
                    request.SessionId,
                    audio,
                    request.ConfirmedTranscript,
                    request.ConfirmationOutcome),
                (eventName, payload) => ForwardEventAsync(request, eventName, payload, turnCancellation.Token),
                turnCancellation.Token);

            var confirmationRequired = state.PendingConfirmations.Remove(request.TurnId);
            if (confirmationRequired)
                _turnRegistry.Release(request.TurnId);
            else
                _turnRegistry.Complete(request.TurnId);
            await Clients.Caller.SendAsync(
                TurnCompletedEvent,
                new SpeakingLiveTurnCompleted(
                    request.SessionId,
                    request.TurnId,
                    request.Sequence,
                    _clock.GetUtcNow()),
                cancellationToken);
            EnglishAiTelemetry.SpeakingLiveTurnDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("outcome", "completed"));
        }
        catch (OperationCanceledException) when (turnCancellation.IsCancellationRequested)
        {
            _turnRegistry.Release(request.TurnId);
            EnglishAiTelemetry.SpeakingLiveTurnDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("outcome", "cancelled"));
        }
        catch (SpeakingMinutesExhaustedException exception)
        {
            // Not a failure: the learner used their daily budget. Marked unrecoverable so the client
            // opens the upgrade prompt instead of retrying a turn that can only fail again today.
            _turnRegistry.Complete(request.TurnId);
            EnglishAiTelemetry.SpeakingLiveErrors.Add(
                1,
                new KeyValuePair<string, object?>("code", SpeakingMinutesExhaustedException.Code));
            await Clients.Caller.SendAsync(
                RecoverableErrorEvent,
                new SpeakingLiveError(
                    request.SessionId,
                    request.TurnId,
                    request.Sequence,
                    SpeakingMinutesExhaustedException.Code,
                    exception.Message,
                    false,
                    _clock.GetUtcNow()),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _turnRegistry.Release(request.TurnId);
            _logger.LogWarning(
                exception,
                "Live speaking turn {TurnId} failed for session {SessionId}.",
                request.TurnId,
                request.SessionId);
            EnglishAiTelemetry.SpeakingLiveErrors.Add(
                1,
                new KeyValuePair<string, object?>("code", "pipeline_failed"));
            await Clients.Caller.SendAsync(
                RecoverableErrorEvent,
                new SpeakingLiveError(
                    request.SessionId,
                    request.TurnId,
                    request.Sequence,
                    "pipeline_failed",
                    "Live speaking is temporarily unavailable.",
                    true,
                    _clock.GetUtcNow()),
                cancellationToken);
        }
        finally
        {
            await state.Gate.WaitAsync(CancellationToken.None);
            try
            {
                if (ReferenceEquals(state.ActiveTurn, turnCancellation))
                    state.ActiveTurn = null;
            }
            finally
            {
                state.Gate.Release();
                turnCancellation.Dispose();
            }
        }
    }

    public async Task InterruptTutor(CancellationToken cancellationToken = default)
    {
        var state = State;
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            state.ActiveTurn?.Cancel();
            EnglishAiTelemetry.SpeakingLiveInterruptions.Add(1);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public async Task LeaveSession(CancellationToken cancellationToken = default)
    {
        var state = State;
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            state.ActiveTurn?.Cancel();
            state.SessionId = null;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    private ConnectionState State => Connections.GetOrAdd(
        Context.ConnectionId,
        _ => new ConnectionState());

    private Task ForwardEventAsync(
        SpeakingLiveTurnRequest request,
        string eventName,
        object payload,
        CancellationToken cancellationToken) => eventName switch
    {
        "recognized" => Clients.Caller.SendAsync(
            FinalTranscriptEvent,
            new SpeakingLiveFinalTranscript(
                request.SessionId,
                request.TurnId,
                request.Sequence,
                ((RecognizedUtteranceEvent)payload).Text,
                _clock.GetUtcNow()),
            cancellationToken),
        "tutor" or "progress" => SendTutorAsync(
            request,
            (TutorUtteranceEvent)payload,
            cancellationToken),
        "pronunciation" => SendPronunciationAsync(
            request,
            (PronunciationUtteranceEvent)payload,
            cancellationToken),
        "audio" => SendAudioAsync(
            request,
            (AudioUtteranceEvent)payload,
            cancellationToken),
        "unrecognized" => Clients.Caller.SendAsync(
            UnrecognizedEvent,
            new SpeakingLiveUnrecognized(
                request.SessionId,
                request.TurnId,
                request.Sequence,
                ((UnrecognizedUtteranceEvent)payload).FeedbackUz,
                ((UnrecognizedUtteranceEvent)payload).RejectionCode,
                _clock.GetUtcNow()),
            cancellationToken),
        "transcript-confirmation-required" => SendTranscriptConfirmationAsync(
            request,
            (TranscriptConfirmationRequiredEvent)payload,
            cancellationToken),
        _ => Task.CompletedTask,
    };

    private async Task SendTranscriptConfirmationAsync(
        SpeakingLiveTurnRequest request,
        TranscriptConfirmationRequiredEvent payload,
        CancellationToken cancellationToken)
    {
        State.PendingConfirmations.Add(request.TurnId);
        await Clients.Caller.SendAsync(
            TranscriptConfirmationRequiredEvent,
            new SpeakingLiveTranscriptConfirmation(
                request.SessionId,
                request.TurnId,
                request.Sequence,
                payload.SuggestedText,
                payload.Alternatives
                    .Select(alternative => new SpeakingLiveTranscriptAlternative(
                        alternative.Text,
                        alternative.Confidence))
                    .ToArray(),
                _clock.GetUtcNow()),
            cancellationToken);
    }

    private Task SendTutorAsync(
        SpeakingLiveTurnRequest request,
        TutorUtteranceEvent payload,
        CancellationToken cancellationToken) => Clients.Caller.SendAsync(
        TutorTextEvent,
        new SpeakingLiveTutorText(
            request.SessionId,
            request.TurnId,
            request.Sequence,
            payload.Text,
            payload.SessionLimitReached,
            payload.NamePrompt,
            payload.TopicProgress,
            payload.Completion,
            _clock.GetUtcNow()),
        cancellationToken);

    private Task SendPronunciationAsync(
        SpeakingLiveTurnRequest request,
        PronunciationUtteranceEvent payload,
        CancellationToken cancellationToken) => Clients.Caller.SendAsync(
        PronunciationReadyEvent,
        new SpeakingLivePronunciation(
            request.SessionId,
            request.TurnId,
            request.Sequence,
            payload.Pronunciation,
            payload.FeedbackUz,
            payload.FocusWord,
            _clock.GetUtcNow()),
        cancellationToken);

    private Task SendAudioAsync(
        SpeakingLiveTurnRequest request,
        AudioUtteranceEvent payload,
        CancellationToken cancellationToken) => Clients.Caller.SendAsync(
        TutorAudioEvent,
        new SpeakingLiveTutorAudio(
            request.SessionId,
            request.TurnId,
            request.Sequence,
            payload.AudioBase64,
            payload.Visemes,
            payload.VisemeAnimation,
            payload.IsNaturalVoice,
            payload.WordTimings,
            _clock.GetUtcNow()),
        cancellationToken);

    private sealed class ConnectionState : IDisposable
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public Guid? SessionId { get; set; }
        public CancellationTokenSource? ActiveTurn { get; set; }
        public HashSet<Guid> PendingConfirmations { get; } = new();

        public void Dispose()
        {
            ActiveTurn?.Cancel();
            ActiveTurn?.Dispose();
            Gate.Dispose();
        }
    }
}
