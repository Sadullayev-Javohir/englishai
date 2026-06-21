namespace Application.Backfill.Models;

/// <summary>
/// One unit of work for the content backfill: a single LLM request that fills one module's content
/// for one topic. <see cref="CustomId"/> uniquely identifies the (module, topic) pair so results -
/// which the Batch API returns in any order - can be routed back to the right backfiller.
/// </summary>
public sealed record BatchContentRequest(
    string CustomId,
    string Model,
    string SystemPrompt,
    string UserPrompt,
    int MaxOutputTokens);
