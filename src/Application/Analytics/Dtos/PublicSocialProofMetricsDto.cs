namespace Application.Analytics.Dtos;

/// <summary>
/// Anonymous, aggregate-only figures safe to display on the public landing page. No learner-level
/// identifiers, revenue, emails or internal segmentation leave this read model.
/// </summary>
public sealed record PublicSocialProofMetricsDto(
    DateOnly AsOf,
    int RegisteredUsers,
    int ActivePremiumUsers,
    int ActiveLearners30d,
    long TotalStudyMinutes,
    long SpeakingSessions,
    long SpeakingMinutes);
