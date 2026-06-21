using System.Security.Cryptography;
using System.Text;
using Domain.Subscription;
using Microsoft.Extensions.Options;

namespace Infrastructure.Subscription;

public interface IPaymentWebhookVerifier
{
    bool Verify(PaymentProvider provider, string transactionId, string status, string signature);
}

public sealed class PaymentWebhookVerifier : IPaymentWebhookVerifier
{
    private readonly RegionalPaymentOptions _options;

    public PaymentWebhookVerifier(IOptions<RegionalPaymentOptions> options) => _options = options.Value;

    public bool Verify(PaymentProvider provider, string transactionId, string status, string signature)
    {
        if (_options.MockMode)
            return true;

        var providerOptions = provider switch
        {
            PaymentProvider.Click => _options.Click,
            PaymentProvider.Payme => _options.Payme,
            PaymentProvider.Uzum => _options.Uzum,
            _ => null,
        };
        if (providerOptions is null || string.IsNullOrWhiteSpace(providerOptions.SecretKey))
            return false;

        var expected = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(providerOptions.SecretKey),
            Encoding.UTF8.GetBytes($"{transactionId}:{status}"))).ToLowerInvariant();
        var supplied = signature.Trim().ToLowerInvariant();
        return supplied.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(supplied), Encoding.ASCII.GetBytes(expected));
    }
}
