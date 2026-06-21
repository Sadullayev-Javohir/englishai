namespace Domain.Speaking;

/// <summary>
/// Central pronunciation scoring thresholds. Kept in one place so the banding rule
/// (and the UI color logic that follows it) stays consistent across the app.
/// </summary>
public static class SpeakingScoreThresholds
{
    /// <summary>At or above this overall score a result is <see cref="PronunciationBand.Good"/>.</summary>
    public const double GoodOverall = 80.0;

    /// <summary>Below this per-word accuracy a word is treated as needing practice.</summary>
    public const double WordNeedsPractice = 80.0;
}
