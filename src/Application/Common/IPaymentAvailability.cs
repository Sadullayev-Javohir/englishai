namespace Application.Common;

/// <summary>
/// Port reporting whether online payments (Click/Payme) are wired up and may be charged. Until they
/// are, only the free plan is offered and the upgrade flow must be refused server-side
/// (<see cref="PaymentsUnavailableException"/>) so Premium is never granted without a real payment.
/// The flag lives in configuration (docs/development-guide.md rule 10/13) and flips on once a real gateway is added.
/// </summary>
public interface IPaymentAvailability
{
    /// <summary>True only when a real payment gateway is configured and subscriptions may be sold.</summary>
    bool PaymentsEnabled { get; }
}
