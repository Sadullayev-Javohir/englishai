using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Infrastructure.Subscription;

public interface IClickShopApiVerifier
{
    bool IsConfigured { get; }
    bool VerifyPrepare(ClickShopApiRequest request);
    bool VerifyComplete(ClickShopApiRequest request);
}

public sealed record ClickShopApiRequest(
    long ClickTransactionId,
    int ServiceId,
    long ClickPaymentId,
    string MerchantTransactionId,
    int? MerchantPrepareId,
    string Amount,
    int Action,
    int Error,
    string ErrorNote,
    string SignTime,
    string SignString);

public sealed class ClickShopApiVerifier : IClickShopApiVerifier
{
    private readonly RegionalPaymentOptions.ProviderOptions _options;

    public ClickShopApiVerifier(IOptions<RegionalPaymentOptions> options) =>
        _options = options.Value.Click;

    public bool IsConfigured =>
        int.TryParse(_options.ServiceId, out _) && !string.IsNullOrWhiteSpace(_options.SecretKey);

    public bool VerifyPrepare(ClickShopApiRequest request) =>
        Verify(request, includePrepareId: false);

    public bool VerifyComplete(ClickShopApiRequest request) =>
        request.MerchantPrepareId.HasValue && Verify(request, includePrepareId: true);

    private bool Verify(ClickShopApiRequest request, bool includePrepareId)
    {
        if (!int.TryParse(_options.ServiceId, out var serviceId) || request.ServiceId != serviceId ||
            string.IsNullOrWhiteSpace(_options.SecretKey))
            return false;

        var signaturePayload = includePrepareId
            ? string.Concat(
                request.ClickTransactionId,
                request.ServiceId,
                _options.SecretKey,
                request.MerchantTransactionId,
                request.MerchantPrepareId,
                request.Amount,
                request.Action,
                request.SignTime)
            : string.Concat(
                request.ClickTransactionId,
                request.ServiceId,
                _options.SecretKey,
                request.MerchantTransactionId,
                request.Amount,
                request.Action,
                request.SignTime);

        var expected = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(signaturePayload)))
            .ToLowerInvariant();
        var supplied = request.SignString.Trim().ToLowerInvariant();
        return supplied.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.ASCII.GetBytes(supplied), Encoding.ASCII.GetBytes(expected));
    }
}
