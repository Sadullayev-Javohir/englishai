namespace Domain.Assessment;

/// <summary>
/// Final outcome of a completed placement test: the overall CEFR level plus the
/// per-stage sub-levels (e.g. "overall B1, but Speaking A2"), as described in
/// PROJECT-SPEC G.1.
/// </summary>
public sealed class PlacementResult
{
    private readonly Dictionary<TestStage, StageResult> _stageResults;

    public PlacementResult(
        CefrLevel overallLevel,
        double overallScore,
        IReadOnlyDictionary<TestStage, StageResult> stageResults)
    {
        OverallLevel = overallLevel;
        OverallScore = overallScore;
        _stageResults = new Dictionary<TestStage, StageResult>(stageResults);
    }

    public CefrLevel OverallLevel { get; }

    /// <summary>Weighted overall score on the 0-100 scale.</summary>
    public double OverallScore { get; }

    public IReadOnlyDictionary<TestStage, StageResult> StageResults => _stageResults;
}
