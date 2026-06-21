using Domain.Subscription;

namespace Application.Common;

/// <summary>
/// Thrown when a free-tier learner hits a feature limit (PROJECT-SPEC H.1). Mapped to
/// HTTP 402 Payment Required by the Web error middleware, signalling that upgrading to
/// Premium removes the limit.
/// </summary>
public sealed class FeatureLimitExceededException : Exception
{
    public FeatureLimitExceededException(PremiumFeature feature, int limit, UsagePeriod period)
        : base($"Free-tier limit reached for {feature}: {limit} per {period}. Upgrade to Premium for unlimited access.")
    {
        Feature = feature;
        Limit = limit;
        Period = period;
    }

    public PremiumFeature Feature { get; }
    public int Limit { get; }
    public UsagePeriod Period { get; }
}
