using Application.Common;

namespace Infrastructure.Subscription;

/// <summary>
/// Configuration-backed <see cref="IPaymentAvailability"/>. Reads <c>Subscription:PaymentsEnabled</c>
/// and defaults to <c>false</c> - so production sells only the free plan until a real Click/Payme
/// gateway is configured and the flag is explicitly turned on.
/// </summary>
public sealed class ConfigPaymentAvailability : IPaymentAvailability
{
    public const string ConfigKey = "Subscription:PaymentsEnabled";

    public ConfigPaymentAvailability(bool paymentsEnabled) => PaymentsEnabled = paymentsEnabled;

    public bool PaymentsEnabled { get; }
}
