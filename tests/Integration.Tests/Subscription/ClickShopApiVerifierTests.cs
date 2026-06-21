using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Infrastructure.Subscription;
using Microsoft.Extensions.Options;

namespace Application.Tests.Subscription;

public sealed class ClickShopApiVerifierTests
{
    [Fact]
    public void Verify_prepare_uses_the_official_click_md5_formula()
    {
        var options = Options.Create(new RegionalPaymentOptions
        {
            Click = new RegionalPaymentOptions.ProviderOptions
            {
                ServiceId = "109430",
                SecretKey = "secret",
            },
        });
        const string payload = "1109430secretclick-order59990.0002026-08-06 12:00:00";
        var signature = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var verifier = new ClickShopApiVerifier(options);

        var valid = verifier.VerifyPrepare(new ClickShopApiRequest(
            1, 109430, 2, "click-order", null, "59990.00", 0, 0, "Success",
            "2026-08-06 12:00:00", signature));

        valid.Should().BeTrue();
    }
}
