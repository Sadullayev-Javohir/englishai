namespace Domain.Speaking;

/// <summary>Who produced a conversation turn.</summary>
public enum ConversationRole
{
    Learner = 1,
    Tutor = 2
}

/// <summary>
/// Pronunciation error categories reported by the assessor (mirrors Azure
/// Pronunciation Assessment error types).
/// </summary>
public enum PronunciationErrorType
{
    None = 0,
    Mispronunciation = 1,
    Omission = 2,
    Insertion = 3
}

/// <summary>
/// Coarse pronunciation quality band, mapped to the design system's semantic
/// colors: <see cref="Good"/> -> success green, <see cref="NeedsImprovement"/> ->
/// soft warning terracotta (never harsh red - DESIGN.md QISM 5).
/// </summary>
public enum PronunciationBand
{
    Good = 1,
    NeedsImprovement = 2
}
