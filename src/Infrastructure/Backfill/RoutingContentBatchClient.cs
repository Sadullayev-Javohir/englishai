using Application.Backfill.Models;
using Application.Backfill.Ports;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Backfill;

/// <summary>
/// Runs a mixed backfill batch through the Hermes gateway - the only LLM backend since 2026-07-28.
/// Every backfill module (vocabulary, reading, grammar, listening, AND writing) runs through it, so
/// the whole catalogue fills at zero cost against a self-hosted model (docs/development-guide.md rule 10). The Claude
/// batch path is not used by the backfill. Requests are dispatched with bounded concurrency
/// (<see cref="MaxConcurrency"/>). Best-effort throughout: a failed request is simply omitted and
/// re-filled on the next run (rules 8, 11); the result map is keyed back by CustomId so ordering is
/// irrelevant.
/// </summary>
public sealed class RoutingContentBatchClient : IContentBatchClient
{
    // Bounded parallelism: 1. The gateway serves the learner-facing request path at the same time, and
    // it applies its own concurrency limit and queue (ResilientHermesGatewayLlmCompletion); a
    // long-running backfill must not sit at the head of that queue ahead of a waiting learner. Slower
    // than parallel, but it completes without starving live traffic.
    private const int MaxConcurrency = 1;

    private readonly ILlmCompletion _llm;
    private readonly ILogger<RoutingContentBatchClient> _logger;

    public RoutingContentBatchClient(
        ILlmCompletion llm,
        ILogger<RoutingContentBatchClient> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, string>> RunAsync(
        IReadOnlyList<BatchContentRequest> requests, CancellationToken cancellationToken)
    {
        var replies = new Dictionary<string, string>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency, CancellationToken = cancellationToken };

        // Lock only the shared dictionary - the per-request LLM call is thread-safe (each takes its own
        // HttpClient from the factory). Failed calls return null and are simply not added.
        await Parallel.ForEachAsync(requests, options, async (request, ct) =>
        {
            try
            {
                var text = await _llm.CompleteAsync(
                    request.SystemPrompt, request.UserPrompt, request.MaxOutputTokens, ct);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lock (replies)
                        replies[request.CustomId] = text!;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Backfill request {CustomId} threw; skipped (re-fillable next run).", request.CustomId);
            }
        });

        return replies;
    }
}
