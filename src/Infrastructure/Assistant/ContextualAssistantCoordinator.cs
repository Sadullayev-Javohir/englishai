using System.Collections.Concurrent;
using Application.Ai;
using Application.Assistant.Ports;
using Application.Video.Ports;
using Infrastructure.Ai;
using Infrastructure.Video;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Assistant;

public sealed class ContextualAssistantCoordinator(
    IContextualAssistant assistant,
    LocalContextualAssistant fallback,
    IAiAdmissionControl admission,
    IAiRequestContextResolver contexts,
    IVideoExplainCache cache,
    ContextualAssistantOptions options,
    ILogger<ContextualAssistantCoordinator> logger) : IContextualAssistantCoordinator
{
    private readonly TimeSpan _replyBudget = TimeSpan.FromSeconds(Math.Clamp(options.RequestTimeoutSeconds, 10, 120));
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _inFlight = new(StringComparer.Ordinal);

    public async Task<string?> ExecuteAsync(ContextualAssistantWork work, string callerKey, CancellationToken cancellationToken)
    {
        var scopedCacheKey = "context:" + callerKey + ":" + work.CacheKey;
        var cached = await cache.GetAsync(scopedCacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached)) return cached;
        var privateKey = $"{callerKey}:{work.CacheKey}";
        var lazy = new Lazy<Task<string?>>(() => ExecuteCoreAsync(work, callerKey, scopedCacheKey, cancellationToken), LazyThreadSafetyMode.ExecutionAndPublication);
        var shared = _inFlight.GetOrAdd(privateKey, lazy);
        try { return await shared.Value.WaitAsync(cancellationToken); }
        finally
        {
            if (ReferenceEquals(shared, lazy) && shared.IsValueCreated && shared.Value.IsCompleted)
                _inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<string?>>>(privateKey, shared));
        }
    }

    private async Task<string?> ExecuteCoreAsync(ContextualAssistantWork work, string callerKey, string scopedCacheKey, CancellationToken cancellationToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(_replyBudget);
        AiRequestContext context;
        try
        {
            context = await contexts.ResolveAsync(AiFeature.Assistant, callerKey, budget.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await UseFallbackAsync(work, null, "request context timed out", cancellationToken);
        }
        using var scope = AiAdmissionContext.Push(new(
            context.CallerKey,
            context.Tier,
            context.Feature,
            AiAdmissionContext.Current.RequestPath));
        string? reply;
        try
        {
            reply = await assistant.AnswerAsync(work.Area, work.Title, work.Context, work.FocusText,
                work.Question, work.History, budget.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await UseFallbackAsync(work, context, $"Hermes timed out after {_replyBudget.TotalSeconds:0} seconds", cancellationToken);
        }
        catch (AiAdmissionException exception)
        {
            return await UseFallbackAsync(work, context, $"Hermes admission failed with {exception.Code}", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not ArgumentException)
        {
            logger.LogWarning(exception, "Hermes contextual assistant failed; using local fallback");
            return await UseFallbackAsync(work, context, "Hermes request failed", cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(reply))
        {
            return await UseFallbackAsync(work, context, "Hermes returned an empty or unsafe reply", cancellationToken);
        }
        var clean = reply.Trim();
        await cache.SetAsync(scopedCacheKey, clean, cancellationToken);
        return clean;
    }

    private async Task<string?> UseFallbackAsync(
        ContextualAssistantWork work,
        AiRequestContext? context,
        string reason,
        CancellationToken cancellationToken)
    {
        if (context is not null)
        {
            admission.RecordProviderUnavailable(context.Feature, context.Tier);
            admission.RecordFallback(context.Feature, context.Tier);
        }
        logger.LogWarning("{Reason}; using contextual assistant fallback", reason);
        return await fallback.AnswerAsync(work.Area, work.Title, work.Context, work.FocusText,
            work.Question, work.History, cancellationToken);
    }

}
