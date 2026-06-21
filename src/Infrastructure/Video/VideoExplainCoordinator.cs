using System.Collections.Concurrent;
using System.Diagnostics;
using Application.Ai;
using Application.Video.Ports;
using Infrastructure.Ai;

namespace Infrastructure.Video;

public sealed class VideoExplainCoordinator : IVideoExplainCoordinator
{
    private const int MaxLatencySamples = 2048;

    private readonly IChatExplainer _explainer;
    private readonly IVideoExplainCache _cache;
    private readonly VideoExplainOptions _options;
    private readonly IAiRequestContextResolver _contexts;
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _inFlight = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<double> _latencies = new();
    private readonly object _circuitLock = new();
    private long _accepted;
    private long _completed;
    private long _cacheHits;
    private long _rejected;
    private long _failed;
    private long _unavailable;
    private long _circuitOpenRejections;
    private int _active;
    private int _queued;
    private int _consecutiveFailures;
    private DateTimeOffset _circuitOpenUntil;

    public VideoExplainCoordinator(
        IChatExplainer explainer,
        IVideoExplainCache cache,
        VideoExplainOptions options,
        IAiRequestContextResolver contexts)
    {
        _explainer = explainer;
        _cache = cache;
        _options = options;
        _contexts = contexts;
    }

    public async Task<string?> ExecuteAsync(
        VideoExplainWork work,
        string callerKey,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync(work.CacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            Interlocked.Increment(ref _cacheHits);
            return cached;
        }

        EnsureCircuitClosed();
        Interlocked.Increment(ref _accepted);

        var lazy = new Lazy<Task<string?>>(
            () => ExecuteLeaderAsync(work, callerKey, cancellationToken),
            LazyThreadSafetyMode.ExecutionAndPublication);
        var shared = _inFlight.GetOrAdd(work.CacheKey, lazy);

        try
        {
            return await shared.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            if (ReferenceEquals(shared, lazy) && shared.IsValueCreated && shared.Value.IsCompleted)
                _inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<string?>>>(work.CacheKey, shared));
        }
    }

    public VideoExplainSnapshot Snapshot()
    {
        var latencies = _latencies.ToArray();
        Array.Sort(latencies);
        var average = latencies.Length == 0 ? 0 : latencies.Average();
        var p95 = latencies.Length == 0 ? 0 : latencies[(int)Math.Min(latencies.Length - 1, Math.Ceiling(latencies.Length * .95) - 1)];
        return new VideoExplainSnapshot(
            Volatile.Read(ref _active),
            Volatile.Read(ref _queued),
            Interlocked.Read(ref _accepted),
            Interlocked.Read(ref _completed),
            Interlocked.Read(ref _cacheHits),
            Interlocked.Read(ref _rejected),
            Interlocked.Read(ref _failed),
            Interlocked.Read(ref _unavailable),
            Interlocked.Read(ref _circuitOpenRejections),
            average,
            p95,
            CircuitOpen);
    }

    private async Task<string?> ExecuteLeaderAsync(VideoExplainWork work, string callerKey, CancellationToken cancellationToken)
    {
        var context = await _contexts.ResolveAsync(AiFeature.VideoExplain, callerKey, cancellationToken);
        using var scope = AiAdmissionContext.Push(new(
            context.CallerKey,
            context.Tier,
            context.Feature,
            AiAdmissionContext.Current.RequestPath));
        Interlocked.Increment(ref _active);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            EnsureCircuitClosed();
            var reply = await _explainer.ExplainAsync(
                work.VideoTitle,
                work.Transcript,
                work.FocusText,
                work.UserMessage,
                work.History,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(reply))
            {
                Interlocked.Increment(ref _unavailable);
                return null;
            }

            ResetCircuit();
            await _cache.SetAsync(work.CacheKey, reply.Trim(), cancellationToken);
            Interlocked.Increment(ref _completed);
            return reply.Trim();
        }
        catch (VideoExplainBusyException)
        {
            throw;
        }
        catch
        {
            RegisterFailure();
            Interlocked.Increment(ref _failed);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            RecordLatency(stopwatch.Elapsed.TotalMilliseconds);
            Interlocked.Decrement(ref _active);
        }
    }

    private bool CircuitOpen
    {
        get
        {
            lock (_circuitLock)
                return _circuitOpenUntil > DateTimeOffset.UtcNow;
        }
    }

    private void EnsureCircuitClosed()
    {
        lock (_circuitLock)
        {
            if (_circuitOpenUntil <= DateTimeOffset.UtcNow)
            {
                if (_circuitOpenUntil != default)
                {
                    _circuitOpenUntil = default;
                    _consecutiveFailures = 0;
                }
                return;
            }
        }

        Interlocked.Increment(ref _circuitOpenRejections);
        throw new VideoExplainBusyException("circuit_open", "AI xizmati vaqtincha band. Birozdan keyin qayta urinib ko'ring.");
    }

    private void RegisterFailure()
    {
        lock (_circuitLock)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= Math.Max(2, _options.CircuitFailureThreshold))
                _circuitOpenUntil = DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(_options.CircuitBreakSeconds, 5, 300));
        }
    }

    private void ResetCircuit()
    {
        lock (_circuitLock)
        {
            _consecutiveFailures = 0;
            _circuitOpenUntil = default;
        }
    }

    private void RecordLatency(double elapsedMs)
    {
        _latencies.Enqueue(elapsedMs);
        while (_latencies.Count > MaxLatencySamples)
            _latencies.TryDequeue(out _);
    }
}
