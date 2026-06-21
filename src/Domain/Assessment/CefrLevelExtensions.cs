namespace Domain.Assessment;

/// <summary>
/// Pure helpers for moving between CEFR levels and the 0-100 skill score scale.
/// Kept free of side effects so the adaptive engine and scoring logic stay fully
/// unit-testable.
/// </summary>
public static class CefrLevelExtensions
{
    /// <summary>The easiest level a question or learner can be placed at.</summary>
    public static CefrLevel Floor => CefrLevel.A1;

    /// <summary>The hardest level a question or learner can be placed at.</summary>
    public static CefrLevel Ceiling => CefrLevel.C2;

    /// <summary>Moves one level harder, clamped at <see cref="CefrLevel.C2"/>.</summary>
    public static CefrLevel StepUp(this CefrLevel level) =>
        level >= Ceiling ? Ceiling : level + 1;

    /// <summary>Moves one level easier, clamped at <see cref="CefrLevel.A1"/>.</summary>
    public static CefrLevel StepDown(this CefrLevel level) =>
        level <= Floor ? Floor : level - 1;

    /// <summary>
    /// Representative point on the 0-100 skill-score scale for a level. Used when
    /// combining per-stage levels into a weighted overall score.
    /// </summary>
    public static int ToScore(this CefrLevel level) => level switch
    {
        CefrLevel.A1 => 20,
        CefrLevel.A2 => 35,
        CefrLevel.B1 => 52,
        CefrLevel.B2 => 68,
        CefrLevel.C1 => 84,
        CefrLevel.C2 => 96,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown CEFR level.")
    };

    /// <summary>
    /// Maps a 0-100 score back to the nearest CEFR level. Thresholds are the
    /// midpoints between the representative scores in <see cref="ToScore"/>.
    /// </summary>
    public static CefrLevel FromScore(double score) => score switch
    {
        < 27.5 => CefrLevel.A1,
        < 43.5 => CefrLevel.A2,
        < 60.0 => CefrLevel.B1,
        < 76.0 => CefrLevel.B2,
        < 90.0 => CefrLevel.C1,
        _ => CefrLevel.C2
    };
}
