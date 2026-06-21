namespace Application.Common;

/// <summary>
/// Thrown when a free-tier learner tries to open or practice a topic beyond their free trial
/// allowance (PROJECT-SPEC H.1 trial gate). Mapped to HTTP 402 Payment Required with the machine code
/// <see cref="Code"/> so the client can distinguish the "subscribe to continue" paywall from a
/// per-feature daily limit (<see cref="FeatureLimitExceededException"/>) and open the upgrade flow.
/// </summary>
public sealed class SubscriptionRequiredException : Exception
{
    /// <summary>Stable machine code returned to the client so it can show the upgrade paywall.</summary>
    public const string CodeValue = "subscription_required";

    public SubscriptionRequiredException(int freeAllowance)
        : base($"Free trial used up: the first {freeAllowance} topics are free. Subscribe to Premium to continue.")
    {
        FreeAllowance = freeAllowance;
    }

    /// <summary>How many topics the free trial covers (for the paywall copy).</summary>
    public int FreeAllowance { get; }

    public string Code => CodeValue;
}
