namespace Application.Common;

/// <summary>
/// Thrown when a learner tries to start a paid subscription while online payments are turned off
/// (<see cref="IPaymentAvailability"/>). Until the Click/Payme gateways are wired up, only the free
/// plan is offered, so the upgrade flow must not silently grant Premium. Mapped to HTTP 503 Service
/// Unavailable with the machine code <see cref="Code"/> so the client can show a "coming soon" notice
/// instead of a generic error.
/// </summary>
public sealed class PaymentsUnavailableException : Exception
{
    /// <summary>Stable machine code returned to the client.</summary>
    public const string CodeValue = "payments_unavailable";

    public PaymentsUnavailableException()
        : base("Online payments are not available yet. Only the free plan can be selected for now.")
    {
    }

    public string Code => CodeValue;
}
