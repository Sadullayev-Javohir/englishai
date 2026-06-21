using Application.Subscription.Ports;
using Domain.Common;
using Domain.Subscription;

namespace Infrastructure.Subscription;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IReadOnlyDictionary<PaymentProvider, IPaymentGateway> _gateways;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(gateway => gateway.Provider);
    }

    public IReadOnlyCollection<PaymentProvider> AvailableProviders => _gateways.Keys.ToArray();

    public IPaymentGateway Resolve(PaymentProvider provider) =>
        _gateways.TryGetValue(provider, out var gateway)
            ? gateway
            : throw new DomainException($"Payment provider '{provider}' is not configured.");
}
