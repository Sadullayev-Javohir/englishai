using Application.Ai;
using Application.Speaking.Ports;
using Infrastructure.Ai;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

internal sealed class AzureStreamingSpeechToTextSession : IStreamingSpeechToTextSession
{
    private readonly AzureSpeechOptions _options;
    private readonly ILogger _logger;
    private readonly IVariableCostMeter _costs;
    private readonly SpeechRecognitionContext _context;
    private readonly Func<string, Task>? _onPartialTranscript;
    private readonly List<IReadOnlyList<SpeechTranscriptionCandidate>> _segments = [];
    private readonly object _segmentsGate = new();
    private readonly TaskCompletionSource<bool> _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(6);
    // Closing the push stream makes Azure flush the final Recognized event and then raise
    // SessionStopped/Canceled(EndOfStream). We wait this long for that natural finalization
    // before forcing a stop, so the final result is not cut off. Kept below the hub's 8s
    // RecognitionTimeout budget (grace + StopTimeout) so a stall surfaces as a retryable
    // timeout instead of racing the cap.
    private static readonly TimeSpan FinalizationGrace = TimeSpan.FromSeconds(1.5);
    private SpeechConfig? _speechConfig;
    private AudioStreamFormat? _format;
    private PushAudioInputStream? _pushStream;
    private AudioConfig? _audioConfig;
    private SpeechRecognizer? _recognizer;
    private long _pcmBytes;
    private volatile string? _lastPartialTranscript;
    private bool _started;
    private bool _completed;

    public AzureStreamingSpeechToTextSession(
        AzureSpeechOptions options,
        ILogger logger,
        IVariableCostMeter costs,
        SpeechRecognitionContext context,
        Func<string, Task>? onPartialTranscript)
    {
        _options = options;
        _logger = logger;
        _costs = costs;
        _context = context;
        _onPartialTranscript = onPartialTranscript;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started) return;
        var admission = AiAdmissionContext.Current;
        _costs.EnsureAllowed(admission.Tier, AiFeature.SpeakingEvaluation, admission.CallerKey);

        _speechConfig = SpeechConfig.FromSubscription(_options.Key, _options.Region);
        _speechConfig.SpeechRecognitionLanguage = string.IsNullOrWhiteSpace(_context.RecognitionLanguage)
            ? _options.RecognitionLanguage
            : _context.RecognitionLanguage;
        _speechConfig.OutputFormat = OutputFormat.Detailed;
        _format = AudioStreamFormat.GetWaveFormatPCM(16_000, 16, 1);
        _pushStream = AudioInputStream.CreatePushStream(_format);
        _audioConfig = AudioConfig.FromStreamInput(_pushStream);
        _recognizer = new SpeechRecognizer(_speechConfig, _audioConfig);

        var phraseList = PhraseListGrammar.FromRecognizer(_recognizer);
        var contextPhrases = ContextPhraseExtractor.Extract(_context);
        foreach (var phrase in contextPhrases) phraseList.AddPhrase(phrase);
        if (contextPhrases.Count > 0)
            phraseList.SetWeight(Math.Max(1.25, _context.IncludeNameHints ? _options.NamePhraseWeight : 1.65));

        _recognizer.Recognizing += (_, eventArgs) =>
        {
            if (string.IsNullOrWhiteSpace(eventArgs.Result.Text)) return;
            // Keep the latest interim hypothesis so CompleteAsync can fall back to it when the
            // stream ends before Azure emits a final Recognized event for the last audio.
            _lastPartialTranscript = eventArgs.Result.Text.Trim();
            if (_onPartialTranscript is null) return;
            _ = _onPartialTranscript(eventArgs.Result.Text.Trim()).ContinueWith(
                task => _logger.LogDebug(task.Exception, "Accent tutor partial transcript callback failed."),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        };
        _recognizer.Recognized += (_, eventArgs) =>
        {
            if (eventArgs.Result.Reason != ResultReason.RecognizedSpeech || string.IsNullOrWhiteSpace(eventArgs.Result.Text)) return;
            var candidates = eventArgs.Result.Best()
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Text))
                .Select(candidate => new SpeechTranscriptionCandidate(candidate.Text.Trim(), candidate.Confidence))
                .DistinctBy(candidate => TranscriptText.Normalize(candidate.Text))
                .ToArray();
            if (candidates.Length > 0)
            {
                lock (_segmentsGate) _segments.Add(candidates);
            }
        };
        _recognizer.SessionStopped += (_, _) => _stopped.TrySetResult(true);
        _recognizer.Canceled += (_, eventArgs) =>
        {
            _logger.LogWarning(
                "Azure streaming speech recognition was cancelled with reason {Reason} and code {ErrorCode}.",
                eventArgs.Reason,
                eventArgs.ErrorCode);
            _stopped.TrySetResult(true);
        };

        await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
        _started = true;
    }

    public Task WriteAsync(ReadOnlyMemory<byte> pcm, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_started || _completed || _pushStream is null)
            throw new InvalidOperationException("Streaming speech session is not active.");
        if (pcm.Length == 0) return Task.CompletedTask;
        var bytes = pcm.ToArray();
        _pushStream.Write(bytes);
        _pcmBytes += bytes.Length;
        return Task.CompletedTask;
    }

    public async Task<SpeechTranscription> CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_completed) throw new InvalidOperationException("Streaming speech session is already completed.");
        _completed = true;
        _pushStream?.Close();
        if (_recognizer is not null)
        {
            // Closing the push stream signals end-of-stream; Azure then flushes the final
            // Recognized event before raising SessionStopped/Canceled. Wait for that natural
            // finalization first, otherwise StopContinuousRecognitionAsync preempts the final
            // result and the turn is left with only partial transcripts.
            await AwaitNaturalFinalizationAsync(cancellationToken).ConfigureAwait(false);
            var stopTask = _recognizer.StopContinuousRecognitionAsync();
            await WaitForStopAsync(stopTask, cancellationToken).ConfigureAwait(false);
        }

        var admission = AiAdmissionContext.Current;
        var audioHours = (_pcmBytes / 32_000d) / 3600d;
        _costs.Record(
            VariableCostCategory.SpeechToText,
            audioHours,
            "audio_hour",
            audioHours * Math.Max(0, _options.SpeechToTextCostPerAudioHourUsd),
            admission.CallerKey,
            AiFeature.SpeakingEvaluation,
            admission.RequestPath);

        IReadOnlyList<SpeechTranscriptionCandidate>[] segments;
        lock (_segmentsGate) segments = _segments.ToArray();
        return StreamingTranscriptComposer.Build(
            segments,
            _lastPartialTranscript,
            _context,
            _options,
            _logger);
    }

    private async Task AwaitNaturalFinalizationAsync(CancellationToken cancellationToken)
    {
        using var grace = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        grace.CancelAfter(FinalizationGrace);
        try
        {
            await _stopped.Task.WaitAsync(grace.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Azure did not finalize within the grace window; fall through to force the stop.
            // Any partial transcript still serves as the fallback in the composer.
        }
    }

    private async Task WaitForStopAsync(Task stopTask, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(StopTimeout);
        try
        {
            await Task.WhenAll(
                stopTask.WaitAsync(timeout.Token),
                _stopped.Task.WaitAsync(timeout.Token)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Azure streaming speech finalization timed out after {TimeoutSeconds} seconds with {PcmBytes} PCM bytes.",
                StopTimeout.TotalSeconds,
                _pcmBytes);
            throw new TimeoutException("Speech recognition finalization timed out.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_started && !_completed && _recognizer is not null)
        {
            _pushStream?.Close();
            try { await _recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false); }
            catch { }
        }
        _recognizer?.Dispose();
        _audioConfig?.Dispose();
        _pushStream?.Dispose();
        _format?.Dispose();
    }
}
