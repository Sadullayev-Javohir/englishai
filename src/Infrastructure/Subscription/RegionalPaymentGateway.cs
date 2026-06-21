using System.Security.Cryptography;
using System.Text;
using Application.Subscription.Ports;
using Domain.Common;
using Domain.Subscription;
using Microsoft.Extensions.Options;

namespace Infrastructure.Subscription;

public sealed class RegionalPaymentGateway : IPaymentGateway
{
    private readonly RegionalPaymentOptions _options;
    private readonly RegionalPaymentOptions.ProviderOptions _provider;

    public RegionalPaymentGateway(PaymentProvider provider, IOptions<RegionalPaymentOptions> options)
    {
        Provider = provider;
        _options = options.Value;
        _provider = provider switch
        {
            PaymentProvider.Click => _options.Click,
            PaymentProvider.Payme => _options.Payme,
            PaymentProvider.Uzum => _options.Uzum,
            _ => throw new ArgumentOutOfRangeException(nameof(provider)),
        };
    }

    public PaymentProvider Provider { get; }

    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken)
    {
        var transactionId = $"{Provider.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";

        if (_options.MockMode)
        {
            var mockUrl = $"/api/subscription/payments/mock-checkout?provider={Provider.ToString().ToLowerInvariant()}&transactionId={transactionId}&returnUrl={Uri.EscapeDataString(request.ReturnUrl)}";
            return Task.FromResult(new PaymentInitiationResult(transactionId, mockUrl));
        }

        if (string.IsNullOrWhiteSpace(_provider.MerchantId) ||
            string.IsNullOrWhiteSpace(_provider.CheckoutUrl) ||
            (Provider == PaymentProvider.Click && string.IsNullOrWhiteSpace(_provider.ServiceId)) ||
            (Provider != PaymentProvider.Click && string.IsNullOrWhiteSpace(_provider.SecretKey)))
            throw new DomainException($"{Provider} merchant credentials are not configured.");

        if (Provider == PaymentProvider.Click)
        {
            var clickCheckoutUrl = BuildClickCheckoutUrl(transactionId, request);
            return Task.FromResult(new PaymentInitiationResult(transactionId, clickCheckoutUrl));
        }

        var payload = string.Join(':', transactionId, request.AmountUzs, _provider.MerchantId);
        var signature = Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(_provider.SecretKey), Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();
        var checkoutUrl = $"{_provider.CheckoutUrl.TrimEnd('/')}?merchant_id={Uri.EscapeDataString(_provider.MerchantId)}&transaction_id={transactionId}&amount={request.AmountUzs}&return_url={Uri.EscapeDataString(request.ReturnUrl)}&callback_url={Uri.EscapeDataString(request.WebhookUrl)}&signature={signature}";

        return Task.FromResult(new PaymentInitiationResult(transactionId, checkoutUrl));
    }

    private string BuildClickCheckoutUrl(string transactionId, PaymentInitiationRequest request)
    {
        var parameters = new Dictionary<string, string>
        {
            ["service_id"] = _provider.ServiceId,
            ["merchant_id"] = _provider.MerchantId,
            ["amount"] = request.AmountUzs.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            ["transaction_param"] = transactionId,
            ["return_url"] = request.ReturnUrl,
        };
        if (!string.IsNullOrWhiteSpace(_provider.MerchantUserId))
            parameters["merchant_user_id"] = _provider.MerchantUserId;

        var query = string.Join("&", parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return $"{_provider.CheckoutUrl.TrimEnd('/')}?{query}";
    }
}
