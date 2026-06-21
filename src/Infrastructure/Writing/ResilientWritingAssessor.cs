using Application.Writing.Ports;
using Domain.Assessment;
using Domain.Writing;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Writing;

/// <summary>
/// Wraps the primary (LLM) writing assessor so a transient provider failure never bubbles up
/// as a request error. On any non-cancellation exception from the inner assessor it falls back
/// to the deterministic offline assessor, keeping flows that must not fail mid-way - the
/// placement test especially - runnable end to end (docs/development-guide.md rules 8, 10, 15). The fallback
/// score is a real, structured assessment (rule 11), just produced without the LLM.
/// </summary>
public sealed class ResilientWritingAssessor : ITopicWritingAssessor
{
    private static readonly TimeSpan PrimaryTimeout = TimeSpan.FromSeconds(35);
    private readonly IWritingAssessor _primary;
    private readonly IWritingAssessor _fallback;
    private readonly ILogger<ResilientWritingAssessor> _logger;

    public ResilientWritingAssessor(
        IWritingAssessor primary,
        IWritingAssessor fallback,
        ILogger<ResilientWritingAssessor> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<WritingAssessment> AssessAsync(
        WritingTask task, string text, CefrLevel assessmentLevel, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PrimaryTimeout);
        try
        {
            return await _primary.AssessAsync(task, text, assessmentLevel, timeout.Token);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Primary writing assessor failed; falling back to the offline assessor.");
            return await _fallback.AssessAsync(task, text, assessmentLevel, cancellationToken);
        }
    }
}
