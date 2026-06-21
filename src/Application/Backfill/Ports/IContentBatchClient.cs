using Application.Backfill.Models;

namespace Application.Backfill.Ports;

/// <summary>
/// Runs a set of content-generation requests and returns each one's raw model reply, keyed by
/// <see cref="BatchContentRequest.CustomId"/>. The production adapter submits them to the Anthropic
/// Batch API (50% cheaper, non-real-time - docs/development-guide.md rule 10); a sequential adapter runs them as
/// ordinary live calls. Either way the orchestration (<see cref="ContentBackfillJob"/>) is identical,
/// so the job is testable with a fake client.
/// </summary>
public interface IContentBatchClient
{
    /// <summary>
    /// Runs the requests and returns <c>CustomId → raw model text</c> for those that completed.
    /// Requests that errored or expired are simply absent from the result (best-effort - the
    /// backfill is idempotent and can be re-run to fill the gaps, docs/development-guide.md rules 8, 11).
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> RunAsync(
        IReadOnlyList<BatchContentRequest> requests, CancellationToken cancellationToken);
}
