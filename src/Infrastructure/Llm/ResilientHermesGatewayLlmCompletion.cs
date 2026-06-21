using System.Diagnostics;
using Application.Ai;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Llm;

public sealed record HermesGatewaySnapshot(int Active, int Queued, long Accepted, long Completed, long Failed, long Rejected, bool CircuitOpen, double AverageLatencyMs);

public sealed class ResilientHermesGatewayLlmCompletion : IConversationLlmCompletion
{
    private const int MaxLatencySamples = 2048;
    private readonly IConversationLlmCompletion _inner;
    private readonly HermesGatewayOptions _options;
    private readonly IAiAdmissionControl _admission;
    private readonly ILogger<ResilientHermesGatewayLlmCompletion> _logger;
    private readonly bool _legacyNullFailures;
    private readonly Queue<double> _latencies = new();
    private readonly object _stateLock = new();
    private long _accepted;
    private long _completed;
    private long _failed;
    private long _rejected;
    private int _active;
    private int _queued;
    private int _consecutiveFailures;
    private DateTimeOffset _circuitOpenUntil;

    public ResilientHermesGatewayLlmCompletion(IConversationLlmCompletion inner, HermesGatewayOptions options, IAiAdmissionControl admission, ILogger<ResilientHermesGatewayLlmCompletion> logger)
    {
        _inner = inner;
        _options = options;
        _admission = admission;
        _logger = logger;
    }

    public ResilientHermesGatewayLlmCompletion(HermesGatewayLlmCompletion inner, HermesGatewayOptions options, ILogger<ResilientHermesGatewayLlmCompletion> logger)
        : this(inner, options, new AiAdmissionControl(new AiAdmissionOptions
        {
            GlobalConcurrency = options.MaxConcurrency,
            ProReservedConcurrency = 0,
            MaxQueueLength = options.MaxQueueLength,
            QueueTimeoutSeconds = options.QueueTimeoutSeconds,
            FreeRequestsPerMinute = int.MaxValue,
            ProRequestsPerMinute = int.MaxValue,
            DailyBudgetUsd = 0,
        }, TimeProvider.System), logger)
    {
        _legacyNullFailures = true;
    }

    public string Model => _inner.Model;

    public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, int maxOutputTokens, CancellationToken cancellationToken = default) =>
        ExecuteAsync(token => _inner.CompleteAsync(systemPrompt, userPrompt, maxOutputTokens, token), EstimateTokens(systemPrompt) + EstimateTokens(userPrompt), maxOutputTokens, cancellationToken);

    public Task<string?> CompleteConversationAsync(string systemPrompt, IReadOnlyList<(bool IsUser, string Text)> turns, int maxOutputTokens, CancellationToken cancellationToken = default) =>
        ExecuteAsync(token => _inner.CompleteConversationAsync(systemPrompt, turns, maxOutputTokens, token), EstimateTokens(systemPrompt) + turns.Sum(turn => EstimateTokens(turn.Text)), maxOutputTokens, cancellationToken);

    public HermesGatewaySnapshot Snapshot()
    {
        lock (_stateLock)
            return new(Volatile.Read(ref _active), Volatile.Read(ref _queued), Interlocked.Read(ref _accepted), Interlocked.Read(ref _completed), Interlocked.Read(ref _failed), Interlocked.Read(ref _rejected), _circuitOpenUntil > DateTimeOffset.UtcNow, _latencies.Count == 0 ? 0 : _latencies.Average());
    }

    private async Task<string?> ExecuteAsync(Func<CancellationToken, Task<string?>> operation, int estimatedInputTokens, int maxOutputTokens, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = AiAdmissionContext.Current;
        if (CircuitOpen)
        {
            Interlocked.Increment(ref _rejected);
            _admission.RecordProviderUnavailable(context.Feature, context.Tier);
            if (_legacyNullFailures) return null;
            throw new AiAdmissionException("provider_unavailable", "AI provider is temporarily unavailable.", Math.Clamp(_options.CircuitBreakSeconds, 5, 300), 503);
        }

        Interlocked.Increment(ref _queued);
        IAiAdmissionLease lease;
        try
        {
            lease = await _admission.AcquireAsync(new AiAdmissionRequest(context.CallerKey, context.Tier, context.Feature, estimatedInputTokens, maxOutputTokens), cancellationToken);
        }
        catch (AiAdmissionException)
        {
            Interlocked.Increment(ref _rejected);
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _queued);
        }

        Interlocked.Increment(ref _accepted);
        Interlocked.Increment(ref _active);
        var stopwatch = Stopwatch.StartNew();
        await using (lease)
        try
        {
            var result = await operation(cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                RegisterFailure();
                Interlocked.Increment(ref _failed);
                _admission.RecordProviderUnavailable(context.Feature, context.Tier);
                if (_legacyNullFailures) return null;
                throw new AiAdmissionException("provider_unavailable", "AI provider returned no usable response.", 5, 503);
            }

            ResetCircuit();
            Interlocked.Increment(ref _completed);
            lease.Complete(EstimateTokens(result));
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (AiAdmissionException) { throw; }
        catch (OperationCanceledException)
        {
            RegisterFailure();
            Interlocked.Increment(ref _failed);
            if (_legacyNullFailures)
            {
                _logger.LogInformation("Hermes gateway request exceeded its transport deadline; the circuit breaker recorded the failure.");
                return null;
            }
            throw new AiAdmissionException("timeout", "AI provider request timed out.", 5, 504);
        }
        catch (Exception exception)
        {
            RegisterFailure();
            Interlocked.Increment(ref _failed);
            _logger.LogWarning(exception, "Hermes gateway request failed; the circuit breaker recorded the failure.");
            _admission.RecordProviderUnavailable(context.Feature, context.Tier);
            if (_legacyNullFailures) return null;
            throw new AiAdmissionException("provider_unavailable", "AI provider is temporarily unavailable.", 5, 503);
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
            lock (_stateLock)
            {
                if (_circuitOpenUntil <= DateTimeOffset.UtcNow)
                {
                    if (_circuitOpenUntil != default) { _circuitOpenUntil = default; _consecutiveFailures = 0; }
                    return false;
                }
                return true;
            }
        }
    }

    private void RegisterFailure()
    {
        lock (_stateLock)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= Math.Max(2, _options.CircuitFailureThreshold))
                _circuitOpenUntil = DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(_options.CircuitBreakSeconds, 5, 300));
        }
    }

    private void ResetCircuit() { lock (_stateLock) { _consecutiveFailures = 0; _circuitOpenUntil = default; } }
    private void RecordLatency(double elapsedMs) { lock (_stateLock) { _latencies.Enqueue(elapsedMs); while (_latencies.Count > MaxLatencySamples) _latencies.Dequeue(); } }
    private static int EstimateTokens(string? text) => string.IsNullOrWhiteSpace(text) ? 0 : Math.Max(1, text.Length / 4);
}
