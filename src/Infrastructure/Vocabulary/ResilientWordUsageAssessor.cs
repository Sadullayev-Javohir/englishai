using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Vocabulary;

/// <summary>
/// Wraps the primary (LLM) word-usage assessor so a transient provider failure never bubbles up
/// as a request error. On any non-cancellation exception from the inner assessor it falls back to
/// the deterministic offline assessor, so submitting a review never fails mid-way just because one
/// grading call hiccupped (docs/development-guide.md rules 8, 10, 15) - same shape as
/// <c>Infrastructure.Writing.ResilientWritingAssessor</c>. The fallback result is still a real,
/// structured assessment (rule 11), just produced without the LLM.
/// </summary>
public sealed class ResilientWordUsageAssessor : IWordUsageAssessor
{
    private readonly IWordUsageAssessor _primary;
    private readonly IWordUsageAssessor _fallback;
    private readonly ILogger<ResilientWordUsageAssessor> _logger;

    public ResilientWordUsageAssessor(
        IWordUsageAssessor primary, IWordUsageAssessor fallback, ILogger<ResilientWordUsageAssessor> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<WordUsageAssessment> AssessAsync(
        string word, string translation, string submittedSentence, CancellationToken cancellationToken)
    {
        try
        {
            return await _primary.AssessAsync(word, translation, submittedSentence, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Primary word-usage assessor failed; falling back to the offline assessor.");
            return await _fallback.AssessAsync(word, translation, submittedSentence, cancellationToken);
        }
    }
}
