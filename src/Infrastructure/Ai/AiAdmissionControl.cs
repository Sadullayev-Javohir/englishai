using System.Collections.Concurrent;
using Application.Ai;

namespace Infrastructure.Ai;

public sealed class AiAdmissionControl : IAiAdmissionControl
{
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(1);
    private readonly AiAdmissionOptions _options;
    private readonly IVariableCostMeter _costs;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _shared;
    private readonly SemaphoreSlim _proReserved;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _callerPermits = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> _rateWindows = new(StringComparer.Ordinal);
    private int _queued;
    private int _active;
    private long _admitted;
    private long _completed;
    private long _rejected;
    private long _timedOut;
    private long _cancelled;
    private long _providerUnavailable;
    private long _fallbacks;
    private long _requests;
    private long _inputTokens;
    private long _outputTokens;

    public AiAdmissionControl(
        AiAdmissionOptions options,
        TimeProvider clock,
        IVariableCostMeter? costs = null)
    {
        _options = options;
        _clock = clock;
        _costs = costs ?? new VariableCostMeter(options, clock);
        var total = Math.Max(1, options.GlobalConcurrency);
        var reserved = Math.Clamp(options.ProReservedConcurrency, 0, total - 1);
        _shared = new SemaphoreSlim(total - reserved);
        _proReserved = new SemaphoreSlim(reserved);
    }

    public async Task<IAiAdmissionLease> AcquireAsync(AiAdmissionRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _costs.EnsureAllowed(request.Tier, request.Feature, request.CallerKey);
        }
        catch (AiAdmissionException)
        {
            Interlocked.Increment(ref _rejected);
            throw;
        }

        if (request.Feature != AiFeature.Translation)
            EnforceRateLimit(request);
        var queued = Interlocked.Increment(ref _queued);
        if (queued > Math.Max(1, _options.MaxQueueLength))
        {
            Interlocked.Decrement(ref _queued);
            Interlocked.Increment(ref _rejected);
            throw new AiAdmissionException("queue_full", "AI queue is full. Please retry shortly.", 5, 429);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.QueueTimeoutSeconds, 1, 180)));
        SemaphoreSlim? callerPermit = null;
        SemaphoreSlim? acquired = null;
        var callerPermitAcquired = false;
        try
        {
            callerPermit = _callerPermits.GetOrAdd(
                request.CallerKey,
                _ => new SemaphoreSlim(Math.Clamp(_options.MaxConcurrentPerCaller, 1, Math.Max(1, _options.GlobalConcurrency))));
            await callerPermit.WaitAsync(timeout.Token);
            callerPermitAcquired = true;
            acquired = await AcquirePermitAsync(request.Tier, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (callerPermitAcquired) callerPermit?.Release();
            Interlocked.Increment(ref _timedOut);
            throw new AiAdmissionException("timeout", "AI queue wait timed out.", 5, 504);
        }
        catch (OperationCanceledException)
        {
            if (callerPermitAcquired) callerPermit?.Release();
            Interlocked.Increment(ref _cancelled);
            throw;
        }
        catch
        {
            if (callerPermitAcquired) callerPermit?.Release();
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _queued);
        }

        if (acquired is null || callerPermit is null)
            throw new InvalidOperationException("AI admission permits were not acquired.");

        Interlocked.Increment(ref _active);
        Interlocked.Increment(ref _admitted);
        Interlocked.Increment(ref _requests);
        Interlocked.Add(ref _inputTokens, Math.Max(0, request.EstimatedInputTokens));
        return new Lease(this, acquired, callerPermit, request);
    }

    public void RecordProviderUnavailable(AiFeature feature, AiSubscriptionTier tier) => Interlocked.Increment(ref _providerUnavailable);
    public void RecordFallback(AiFeature feature, AiSubscriptionTier tier) => Interlocked.Increment(ref _fallbacks);

    public AiAdmissionSnapshot Snapshot()
    {
        var budget = _costs.Snapshot();
        var aiCost = budget.Categories
            .FirstOrDefault(item => item.Category == VariableCostCategory.Ai)?.EstimatedCostUsd ?? 0;
        return new AiAdmissionSnapshot(
            Volatile.Read(ref _active), Volatile.Read(ref _queued), Interlocked.Read(ref _admitted),
            Interlocked.Read(ref _completed), Interlocked.Read(ref _rejected), Interlocked.Read(ref _timedOut),
            Interlocked.Read(ref _cancelled), Interlocked.Read(ref _providerUnavailable), Interlocked.Read(ref _fallbacks),
            Interlocked.Read(ref _requests), Interlocked.Read(ref _inputTokens), Interlocked.Read(ref _outputTokens),
            aiCost,
            budget.DailyBudgetUsedUsd,
            budget.DailyBudgetLimitUsd,
            budget.DailyBudgetWarning,
            budget.FreeTierShed,
            budget.PricingConfigured);
    }

    private async Task<SemaphoreSlim> AcquirePermitAsync(AiSubscriptionTier tier, CancellationToken cancellationToken)
    {
        if (tier == AiSubscriptionTier.Free || _proReserved.CurrentCount == 0)
        {
            await _shared.WaitAsync(cancellationToken);
            return _shared;
        }

        if (await _shared.WaitAsync(0, cancellationToken)) return _shared;
        await _proReserved.WaitAsync(cancellationToken);
        return _proReserved;
    }

    private void EnforceRateLimit(AiAdmissionRequest request)
    {
        var limit = request.Feature == AiFeature.Assistant
            ? request.Tier == AiSubscriptionTier.Pro
                ? Math.Max(1, _options.AssistantProRequestsPerMinute)
                : Math.Max(1, _options.AssistantFreeRequestsPerMinute)
            : request.Tier == AiSubscriptionTier.Pro
                ? Math.Max(1, _options.ProRequestsPerMinute)
                : Math.Max(1, _options.FreeRequestsPerMinute);
        var now = _clock.GetUtcNow();
        var key = $"{request.Tier}:{request.Feature}:{request.CallerKey}";
        var window = _rateWindows.GetOrAdd(key, _ => new ConcurrentQueue<DateTimeOffset>());
        while (window.TryPeek(out var timestamp) && now - timestamp >= RateWindow) window.TryDequeue(out _);
        if (window.Count >= limit)
        {
            Interlocked.Increment(ref _rejected);
            var retryAfter = window.TryPeek(out var oldest) ? Math.Max(1, (int)Math.Ceiling((oldest + RateWindow - now).TotalSeconds)) : 60;
            throw new AiAdmissionException("rate_limited", "AI request limit reached. Please retry later.", retryAfter, 429);
        }
        window.Enqueue(now);
    }

    private void Complete(AiAdmissionRequest request, int outputTokens)
    {
        var boundedOutput = Math.Clamp(outputTokens, 0, Math.Max(0, request.MaxOutputTokens));
        Interlocked.Add(ref _outputTokens, boundedOutput);
        var cost = request.EstimatedInputTokens * _options.EstimatedInputCostPerMillionTokensUsd / 1_000_000d
                   + boundedOutput * _options.EstimatedOutputCostPerMillionTokensUsd / 1_000_000d;
        var context = AiAdmissionContext.Current;
        _costs.Record(
            VariableCostCategory.Ai,
            request.EstimatedInputTokens + boundedOutput,
            "token",
            cost,
            request.CallerKey,
            request.Feature,
            context.RequestPath);
        Interlocked.Increment(ref _completed);
    }

    private void Release(SemaphoreSlim semaphore, SemaphoreSlim callerPermit)
    {
        Interlocked.Decrement(ref _active);
        semaphore.Release();
        callerPermit.Release();
    }

    private sealed class Lease(
        AiAdmissionControl owner,
        SemaphoreSlim semaphore,
        SemaphoreSlim callerPermit,
        AiAdmissionRequest request) : IAiAdmissionLease
    {
        private int _disposed;
        private int _completed;
        public AiAdmissionRequest Request { get; } = request;

        public void Complete(int estimatedOutputTokens)
        {
            if (Interlocked.Exchange(ref _completed, 1) == 0) owner.Complete(Request, estimatedOutputTokens);
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.Release(semaphore, callerPermit);
            return ValueTask.CompletedTask;
        }
    }
}
