using MediatR;

namespace Application.Speaking.GetSpeakingQuota;

/// <summary>
/// How much of the learner's daily speaking budget is left. Read before entering the conversation
/// room so the paywall opens up front rather than after they have already spoken a sentence.
/// </summary>
public sealed record GetSpeakingQuotaQuery(Guid LearnerId) : IRequest<SpeakingQuotaStatusDto>;

/// <param name="ResetsAt">The learner's next local midnight, when the budget renews.</param>
public sealed record SpeakingQuotaStatusDto(
    double LimitMinutes,
    double UsedMinutes,
    double RemainingMinutes,
    bool IsAllowed,
    bool IsPremium,
    DateTimeOffset ResetsAt);
