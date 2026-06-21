using Domain.Assessment;
using Domain.Common;

namespace Domain.Writing;

public enum WritingAssessmentSource
{
    Hermes = 0,
    LocalFallback = 1,
}

/// <summary>The 1-5 score for a single writing dimension.</summary>
public sealed record DimensionScore(WritingDimension Dimension, int Score);

/// <summary>
/// The structured result of assessing a writing submission across the four G.3 dimensions.
/// The LLM produces the raw per-dimension scores (1-5) and the issue list; this domain type
/// owns the deterministic scoring logic - the weighted overall band, its 0-100 projection
/// (fed to the Writing skill score, G.4) and the estimated CEFR level. Building it validates
/// that all four dimensions are present and in range, so an ill-formed LLM response is
/// rejected rather than silently mis-scored (docs/development-guide.md rule 11 - structured, not free-form).
/// </summary>
public sealed class WritingAssessment
{
    public const int MinDimensionScore = 1;
    public const int MaxDimensionScore = 5;

    private static readonly WritingDimension[] RequiredDimensions =
    {
        WritingDimension.TaskAchievement,
        WritingDimension.Coherence,
        WritingDimension.LexicalResource,
        WritingDimension.GrammaticalAccuracy,
    };

    private WritingAssessment(
        Guid taskId,
        IReadOnlyList<DimensionScore> dimensionScores,
        IReadOnlyList<WritingIssue> issues,
        WritingAssessmentSource source)
    {
        TaskId = taskId;
        DimensionScores = dimensionScores;
        Issues = issues;
        Source = source;
    }

    public Guid TaskId { get; }

    /// <summary>The 1-5 score for each of the four dimensions.</summary>
    public IReadOnlyList<DimensionScore> DimensionScores { get; }

    /// <summary>The located issues, each with a code resolvable to a vetted Uzbek explanation.</summary>
    public IReadOnlyList<WritingIssue> Issues { get; }

    public WritingAssessmentSource Source { get; }

    /// <summary>The equal-weighted average of the four dimension scores (1.0-5.0).</summary>
    public double OverallBand => DimensionScores.Average(d => d.Score);

    /// <summary>
    /// The overall band projected onto a 0-100 scale (score 1 → 0, score 5 → 100), used to
    /// record the Writing skill activity that feeds the level-transition formula (G.4).
    /// </summary>
    public int OverallPercent =>
        (int)Math.Round((OverallBand - MinDimensionScore) / (MaxDimensionScore - MinDimensionScore) * 100);

    /// <summary>
    /// The raw estimated CEFR level implied by the overall band (PROJECT-SPEC G.3), ignoring the
    /// task's level. Used by the placement test, which deliberately probes the learner's ceiling.
    /// Topic writing tasks should use <see cref="EstimateLevelCappedTo"/> instead.
    /// </summary>
    public CefrLevel EstimatedLevel => OverallBand switch
    {
        >= 4.5 => CefrLevel.C2,
        >= 4.0 => CefrLevel.C1,
        >= 3.5 => CefrLevel.B2,
        >= 3.0 => CefrLevel.B1,
        >= 2.0 => CefrLevel.A2,
        _ => CefrLevel.A1,
    };

    /// <summary>
    /// The estimated level capped so it can exceed the task's own level by at most one band. A
    /// strong A1 submission is reported as at most A2 - an A1 prompt never demands C2-level
    /// production, so grading it C2 is meaningless and misleads the learner. The cap never lowers
    /// a weak submission's estimate; it only prevents an inflated ceiling.
    /// </summary>
    public CefrLevel EstimateLevelCappedTo(CefrLevel taskLevel)
    {
        var ceiling = (CefrLevel)Math.Min((int)CefrLevel.C2, (int)taskLevel + 1);
        return EstimatedLevel < ceiling ? EstimatedLevel : ceiling;
    }

    public static WritingAssessment Create(
        Guid taskId,
        IEnumerable<DimensionScore> dimensionScores,
        IEnumerable<WritingIssue> issues,
        WritingAssessmentSource source = WritingAssessmentSource.Hermes)
    {
        if (taskId == Guid.Empty)
            throw new DomainException("Assessment must reference a task.");

        var scores = dimensionScores?.ToList() ?? new List<DimensionScore>();

        foreach (var required in RequiredDimensions)
        {
            var matches = scores.Where(s => s.Dimension == required).ToList();
            if (matches.Count != 1)
                throw new DomainException($"Assessment must score the {required} dimension exactly once.");
            if (matches[0].Score is < MinDimensionScore or > MaxDimensionScore)
                throw new DomainException($"{required} score must be between {MinDimensionScore} and {MaxDimensionScore}.");
        }

        if (scores.Count != RequiredDimensions.Length)
            throw new DomainException("Assessment must score exactly the four required dimensions.");

        var issueList = issues?.ToList() ?? new List<WritingIssue>();
        foreach (var issue in issueList)
        {
            if (issue is null)
                throw new DomainException("Issue must not be null.");
            if (string.IsNullOrWhiteSpace(issue.IssueCode))
                throw new DomainException("Issue code must not be empty.");
        }

        return new WritingAssessment(taskId, scores, issueList, source);
    }
}
