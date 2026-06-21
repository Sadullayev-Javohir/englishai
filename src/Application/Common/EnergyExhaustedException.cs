namespace Application.Common;

/// <summary>
/// Thrown when a learner tries to start a Video or Speaking activity with an empty energy bar.
/// Mapped to HTTP 409 Conflict with the machine code <see cref="CodeValue"/> - deliberately not
/// 402, because 402 makes the client open the upgrade paywall and an empty bar is a
/// "wait for the refill" state, not an upsell. The client opens the energy balance modal instead.
/// </summary>
public sealed class EnergyExhaustedException : Exception
{
    /// <summary>Stable machine code the client keys the energy modal off.</summary>
    public const string CodeValue = "energy_exhausted";

    public EnergyExhaustedException(DateTimeOffset? nextRefillAt)
        : base("Energy is used up. The next unit is restored automatically.")
    {
        NextRefillAt = nextRefillAt;
    }

    /// <summary>When the next single unit lands, so the client can show the countdown.</summary>
    public DateTimeOffset? NextRefillAt { get; }

    public string Code => CodeValue;
}
