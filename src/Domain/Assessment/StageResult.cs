namespace Domain.Assessment;

/// <summary>
/// Per-stage outcome of a finalized placement test: the estimated CEFR level for
/// that skill area and its equivalent 0-100 score.
/// </summary>
public sealed record StageResult(TestStage Stage, CefrLevel Level, int Score);
