using System.Collections.Concurrent;
using Application.Speaking;
using Application.Speaking.AccentTutors;
using Application.Speaking.Ports;
using Microsoft.AspNetCore.SignalR;

namespace Web.Hubs;

public sealed class AccentTutorLiveHub(
    IStreamingSpeechToTextService streamingSpeech,
    IUtteranceEndpointDetector endpointDetector,
    IServiceScopeFactory scopes,
    IHubContext<AccentTutorLiveHub> hubContext,
    TimeProvider clock,
    ILogger<AccentTutorLiveHub> logger) : Hub
{
    private const int MaximumPcmBytes = 960_000; // 30 s, 16 kHz, 16-bit mono
    private static readonly TimeSpan RecognitionTimeout = TimeSpan.FromSeconds(8);
    // Bounds the LLM reply + pronunciation + TTS stage so a hung provider call surfaces a clean
    // recoverable error instead of leaving the turn stuck until the client-side 15 s guard fires.
    private static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(25);
    private static readonly ConcurrentDictionary<string, ConnectionState> Connections = new();

    public override Task OnConnectedAsync()
    {
        Connections.TryAdd(Context.ConnectionId, new ConnectionState());
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var state)) await state.DisposeAsync();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<AccentTutorLiveSessionReady> JoinSession(string tutorId)
    {
        AccentTutorStartQueryHandler.EnsureTutor(tutorId);
        var state = State;
        await state.Gate.WaitAsync(Context.ConnectionAborted);
        try { state.TutorId = tutorId; }
        finally { state.Gate.Release(); }
        return new AccentTutorLiveSessionReady(tutorId, clock.GetUtcNow());
    }

    public async Task LeaveSession()
    {
        if (Connections.TryRemove(Context.ConnectionId, out var state)) await state.DisposeAsync();
    }

    public async Task StartTurn(AccentTutorLiveStartTurnRequest request)
    {
        var state = State;
        await state.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            EnsureJoined(state, request.TutorId);
            await state.CancelTurnAsync();
            state.TurnId = request.TurnId;
            state.Sequence = request.Sequence;
            state.History = request.History ?? [];
            state.IsInterruption = request.IsInterruption;
            state.StartedAt = clock.GetUtcNow();
            state.LastTranscriptChangedAt = state.StartedAt;
            state.Pcm = new MemoryStream();
            var connectionId = Context.ConnectionId;
            state.SpeechSession = streamingSpeech.CreateSession(
                AccentTutorRecognitionContext.Create(request.TutorId, state.History),
                text => OnPartialTranscriptAsync(connectionId, request, text));
            await state.SpeechSession.StartAsync(Context.ConnectionAborted);
        }
        finally { state.Gate.Release(); }

        await Clients.Caller.SendAsync("TurnStarted", Event(request), Context.ConnectionAborted);
    }

    public async Task AppendAudioChunk(AccentTutorLiveAudioChunkRequest request)
    {
        byte[] pcm;
        try { pcm = Convert.FromBase64String(request.PcmBase64); }
        catch (FormatException) { throw new HubException("Audio chunk is invalid."); }
        if (pcm.Length is 0 or > 64_000 || pcm.Length % 2 != 0)
            throw new HubException("Audio chunk size is invalid.");

        var state = State;
        await state.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            EnsureTurn(state, request.TutorId, request.TurnId);
            if (state.Committing) return;
            state.CancelPendingCommit();
            if (state.Pcm!.Length + pcm.Length > MaximumPcmBytes)
                throw new HubException("Maximum turn duration was reached.");
            await state.Pcm.WriteAsync(pcm, Context.ConnectionAborted);
            await state.SpeechSession!.WriteAsync(pcm, Context.ConnectionAborted);
        }
        finally { state.Gate.Release(); }
        await Clients.Caller.SendAsync("AudioChunkAccepted", Event(request), Context.ConnectionAborted);
    }

    public async Task SpeechPaused(AccentTutorLiveTurnSignal request)
    {
        var state = State;
        await state.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            EnsureTurn(state, request.TutorId, request.TurnId);
            if (state.Committing) return;
            state.PausedAt = clock.GetUtcNow();
            state.CancelPendingCommit();
            state.PendingCommit = new CancellationTokenSource();
            _ = DetectEndpointAsync(Context.ConnectionId, state, request, state.PendingCommit.Token);
        }
        finally { state.Gate.Release(); }
        await Clients.Caller.SendAsync("SpeechPauseCandidate", Event(request), Context.ConnectionAborted);
    }

    public async Task ResumeSpeech(AccentTutorLiveTurnSignal request)
    {
        var state = State;
        await state.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            EnsureTurn(state, request.TutorId, request.TurnId);
            if (state.Committing) return;
            state.CancelPendingCommit();
            state.PausedAt = null;
        }
        finally { state.Gate.Release(); }
        await Clients.Caller.SendAsync("SpeechResumed", Event(request), Context.ConnectionAborted);
    }

    public Task CommitTurn(AccentTutorLiveTurnSignal request) =>
        CommitTurnAsync(Context.ConnectionId, State, request, Context.ConnectionAborted);

    public async Task CancelTurn(AccentTutorLiveTurnSignal request)
    {
        var state = State;
        await state.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            EnsureTurn(state, request.TutorId, request.TurnId);
            await state.CancelTurnAsync();
        }
        finally { state.Gate.Release(); }
    }

    private async Task OnPartialTranscriptAsync(string connectionId, AccentTutorLiveStartTurnRequest request, string text)
    {
        if (!Connections.TryGetValue(connectionId, out var state)) return;
        await state.Gate.WaitAsync();
        try
        {
            if (state.TurnId != request.TurnId) return;
            if (!string.Equals(state.PartialTranscript, text, StringComparison.Ordinal))
            {
                state.PartialTranscript = text;
                state.LastTranscriptChangedAt = clock.GetUtcNow();
                state.CancelPendingCommit();
                if (state.PausedAt is not null)
                {
                    state.PendingCommit = new CancellationTokenSource();
                    _ = DetectEndpointAsync(connectionId, state, new(request.TutorId, request.TurnId, request.Sequence), state.PendingCommit.Token);
                }
            }
        }
        finally { state.Gate.Release(); }
        await hubContext.Clients.Client(connectionId).SendAsync(
            "PartialTranscript",
            new AccentTutorLivePartialTranscript(request.TutorId, request.TurnId, request.Sequence, text, clock.GetUtcNow()));
    }

    private async Task DetectEndpointAsync(
        string connectionId,
        ConnectionState state,
        AccentTutorLiveTurnSignal request,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(150, cancellationToken);
                UtteranceEndpointSnapshot snapshot;
                await state.Gate.WaitAsync(cancellationToken);
                try
                {
                    if (state.TurnId != request.TurnId || state.PausedAt is null) return;
                    var now = clock.GetUtcNow();
                    snapshot = new(
                        now - state.PausedAt.Value,
                        state.PartialTranscript,
                        now - state.LastTranscriptChangedAt,
                        now - state.StartedAt);
                }
                finally { state.Gate.Release(); }

                var decision = await endpointDetector.DetectAsync(snapshot, cancellationToken);
                if (decision == UtteranceEndpointDecision.Discard)
                {
                    await DiscardTurnAsync(connectionId, state, request, "no_recognized_speech", cancellationToken);
                    return;
                }
                if (decision == UtteranceEndpointDecision.Commit)
                {
                    await CommitTurnAsync(connectionId, state, request, cancellationToken);
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Accent tutor endpoint detection failed for turn {TurnId}.", request.TurnId);
        }
    }

    private async Task DiscardTurnAsync(
        string connectionId,
        ConnectionState state,
        AccentTutorLiveTurnSignal request,
        string reason,
        CancellationToken cancellationToken)
    {
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            if (state.Committing) return;
            EnsureTurn(state, request.TutorId, request.TurnId);
            await state.CancelTurnAsync();
        }
        finally { state.Gate.Release(); }
        logger.LogInformation(
            "Accent tutor turn {TurnId} was discarded with reason {Reason}.",
            request.TurnId,
            reason);
        await hubContext.Clients.Client(connectionId).SendAsync(
            "TurnDiscarded",
            new AccentTutorLiveTurnDiscarded(
                request.TutorId,
                request.TurnId,
                request.Sequence,
                reason,
                clock.GetUtcNow()),
            cancellationToken);
    }

    private async Task CommitTurnAsync(
        string connectionId,
        ConnectionState state,
        AccentTutorLiveTurnSignal request,
        CancellationToken cancellationToken)
    {
        IStreamingSpeechToTextSession speechSession;
        byte[] pcm;
        IReadOnlyList<AccentTutorMessage> history;
        bool isInterruption;
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            EnsureTurn(state, request.TutorId, request.TurnId);
            if (state.Committing) return;
            state.Committing = true;
            state.CancelPendingCommit();
            state.PausedAt = null;
            speechSession = state.SpeechSession!;
            pcm = state.Pcm!.ToArray();
            history = state.History;
            isInterruption = state.IsInterruption;
            logger.LogInformation(
                "Accent tutor turn {TurnId} is committing with {PcmDurationMs:F0} ms audio and partial transcript {HasPartialTranscript}.",
                request.TurnId,
                pcm.Length / 32d,
                !string.IsNullOrWhiteSpace(state.PartialTranscript));
        }
        finally { state.Gate.Release(); }

        await hubContext.Clients.Client(connectionId).SendAsync("TurnCommitted", Event(request), cancellationToken);
        try
        {
            using var recognitionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            recognitionTimeout.CancelAfter(RecognitionTimeout);
            SpeechTranscription transcription;
            try
            {
                transcription = await speechSession.CompleteAsync(recognitionTimeout.Token);
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested &&
                recognitionTimeout.IsCancellationRequested)
            {
                throw new TimeoutException("Speech recognition timed out.");
            }
            using var scope = scopes.CreateScope();
            var turns = scope.ServiceProvider.GetRequiredService<AccentTutorTurnCommandHandler>();
            var command = new AccentTutorTurnCommand(request.TutorId, EncodePcmWav(pcm), history, isInterruption);
            using var replyTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            replyTimeout.CancelAfter(ReplyTimeout);
            try
            {
                // WaitAsync guarantees the await returns on timeout even though the provider's
                // synchronous call may ignore the cancellation token; the abandoned work drains
                // in the background while the turn recovers.
                await turns.ProcessRecognizedAsync(
                    command,
                    transcription,
                    (eventName, payload) => ForwardAsync(connectionId, request, eventName, payload, cancellationToken),
                    replyTimeout.Token)
                    .WaitAsync(replyTimeout.Token);
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested &&
                replyTimeout.IsCancellationRequested)
            {
                throw new TimeoutException("Accent tutor reply timed out.");
            }
            await hubContext.Clients.Client(connectionId).SendAsync(
                "TurnCompleted",
                new AccentTutorLiveTurnCompleted(
                    request.TutorId,
                    request.TurnId,
                    request.Sequence,
                    clock.GetUtcNow()),
                CancellationToken.None);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Accent tutor live turn {TurnId} failed.", request.TurnId);
            var (code, retryable) = exception switch
            {
                SpeakingTutorUnavailableException tutorError => (tutorError.Code, tutorError.Retryable),
                TimeoutException when exception.Message.StartsWith("Accent tutor reply", StringComparison.Ordinal)
                    => ("accent_tutor_timeout", true),
                TimeoutException => ("speech_recognition_timeout", true),
                _ when exception.Message.StartsWith("Speech could not be recognized", StringComparison.Ordinal)
                    => ("speech_not_recognized", true),
                _ => ("accent_tutor_failed", true),
            };
            await hubContext.Clients.Client(connectionId).SendAsync(
                "RecoverableError",
                new AccentTutorLiveError(request.TutorId, request.TurnId, request.Sequence,
                    code, exception.Message, retryable, clock.GetUtcNow()),
                CancellationToken.None);
        }
        finally
        {
            await state.Gate.WaitAsync();
            try { if (state.TurnId == request.TurnId) await state.CancelTurnAsync(); }
            finally { state.Gate.Release(); }
        }
    }

    private Task ForwardAsync(string connectionId, AccentTutorLiveTurnSignal request, string eventName, object payload, CancellationToken cancellationToken)
    {
        var client = hubContext.Clients.Client(connectionId);
        return eventName switch
        {
            "recognized" when payload is AccentTutorRecognizedEvent value => client.SendAsync("FinalTranscript", new AccentTutorLiveFinalTranscript(request.TutorId, request.TurnId, request.Sequence, value.Transcript, clock.GetUtcNow()), cancellationToken),
            "tutor" when payload is AccentTutorReplyEvent value => client.SendAsync("TutorText", new AccentTutorLiveTutorText(request.TutorId, request.TurnId, request.Sequence, value.TutorText, clock.GetUtcNow()), cancellationToken),
            "pronunciation" when payload is AccentTutorPronunciationEvent value => client.SendAsync("PronunciationReady", new AccentTutorLivePronunciation(request.TutorId, request.TurnId, request.Sequence, value.Pronunciation, clock.GetUtcNow()), cancellationToken),
            "audio" when payload is AccentTutorAudioEvent value => client.SendAsync("TutorAudio", new AccentTutorLiveTutorAudio(request.TutorId, request.TurnId, request.Sequence, value.TutorAudioBase64, value.IsNaturalVoice, value.WordTimings, clock.GetUtcNow()), cancellationToken),
            _ => Task.CompletedTask,
        };
    }

    private AccentTutorLiveLifecycleEvent Event(AccentTutorLiveTurnSignal request) =>
        new(request.TutorId, request.TurnId, request.Sequence, clock.GetUtcNow());
    private AccentTutorLiveLifecycleEvent Event(AccentTutorLiveStartTurnRequest request) =>
        new(request.TutorId, request.TurnId, request.Sequence, clock.GetUtcNow());
    private AccentTutorLiveLifecycleEvent Event(AccentTutorLiveAudioChunkRequest request) =>
        new(request.TutorId, request.TurnId, request.Sequence, clock.GetUtcNow());

    private ConnectionState State => Connections.GetOrAdd(Context.ConnectionId, _ => new ConnectionState());

    private static void EnsureJoined(ConnectionState state, string tutorId)
    {
        AccentTutorStartQueryHandler.EnsureTutor(tutorId);
        if (state.TutorId != tutorId) throw new HubException("Join the accent tutor session before starting a turn.");
    }

    private static void EnsureTurn(ConnectionState state, string tutorId, Guid turnId)
    {
        EnsureJoined(state, tutorId);
        if (state.TurnId != turnId || state.SpeechSession is null) throw new HubException("Start the turn before sending audio.");
    }

    private static byte[] EncodePcmWav(byte[] pcm)
    {
        using var stream = new MemoryStream(44 + pcm.Length);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + pcm.Length);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(16_000);
        writer.Write(32_000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(pcm.Length);
        writer.Write(pcm);
        return stream.ToArray();
    }

    private sealed class ConnectionState : IAsyncDisposable
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public string? TutorId { get; set; }
        public Guid? TurnId { get; set; }
        public long Sequence { get; set; }
        public IReadOnlyList<AccentTutorMessage> History { get; set; } = [];
        public bool IsInterruption { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset LastTranscriptChangedAt { get; set; }
        public DateTimeOffset? PausedAt { get; set; }
        public string PartialTranscript { get; set; } = string.Empty;
        public MemoryStream? Pcm { get; set; }
        public IStreamingSpeechToTextSession? SpeechSession { get; set; }
        public CancellationTokenSource? PendingCommit { get; set; }
        public bool Committing { get; set; }

        public void CancelPendingCommit()
        {
            PendingCommit?.Cancel();
            PendingCommit?.Dispose();
            PendingCommit = null;
        }

        public async Task CancelTurnAsync()
        {
            CancelPendingCommit();
            if (SpeechSession is not null) await SpeechSession.DisposeAsync();
            Pcm?.Dispose();
            TurnId = null;
            SpeechSession = null;
            Pcm = null;
            PartialTranscript = string.Empty;
            PausedAt = null;
            Committing = false;
        }

        public async ValueTask DisposeAsync()
        {
            await Gate.WaitAsync();
            try { await CancelTurnAsync(); }
            finally { Gate.Release(); Gate.Dispose(); }
        }
    }
}
