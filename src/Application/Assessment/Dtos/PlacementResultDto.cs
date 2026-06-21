using Domain.Assessment;

namespace Application.Assessment.Dtos;

public sealed record StageResultDto(TestStage Stage, CefrLevel Level, int Score);

/// <summary>Client-facing view of a finalized placement test result.</summary>
public sealed record PlacementResultDto(
    CefrLevel OverallLevel,
    double OverallScore,
    IReadOnlyList<StageResultDto> StageResults)
{
    public static PlacementResultDto FromDomain(PlacementResult result) =>
        new(
            result.OverallLevel,
            Math.Round(result.OverallScore, 1),
            result.StageResults.Values
                .OrderBy(s => s.Stage)
                .Select(s => new StageResultDto(s.Stage, s.Level, s.Score))
                .ToList());
}
