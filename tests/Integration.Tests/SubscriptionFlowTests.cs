using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end purchase flow over HTTP (PROJECT-SPEC Qism H): start checkout → confirm
/// payment (webhook) → Premium active → cancel. Runs on the in-memory adapters and the
/// Local payment gateway (no real provider).
/// </summary>
public class SubscriptionFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SubscriptionFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Purchase_flow_activates_then_cancels_premium()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        // Default state: free.
        var initial = await GetJsonAsync(client, $"/api/subscription/{learnerId}");
        initial.GetProperty("status").GetInt32().Should().Be(0); // Free
        initial.GetProperty("isPremiumActive").GetBoolean().Should().BeFalse();

        // Start a monthly purchase.
        var startResponse = await client.PostAsJsonAsync(
            $"/api/subscription/{learnerId}/start", new { plan = 0, provider = 1 });
        startResponse.EnsureSuccessStatusCode();
        var start = await ReadJsonAsync(startResponse);
        var transactionId = start.GetProperty("transactionId").GetString();
        transactionId.Should().NotBeNullOrWhiteSpace();
        start.GetProperty("amountUzs").GetInt32().Should().BeGreaterThan(0);

        // Confirm the payment (simulating the provider webhook).
        var confirmResponse = await client.PostAsJsonAsync(
            "/api/subscription/payments/confirm", new { transactionId });
        confirmResponse.EnsureSuccessStatusCode();
        var confirmed = await ReadJsonAsync(confirmResponse);
        confirmed.GetProperty("status").GetInt32().Should().Be(1); // Premium
        confirmed.GetProperty("isPremiumActive").GetBoolean().Should().BeTrue();

        // Status reflects Premium.
        var afterConfirm = await GetJsonAsync(client, $"/api/subscription/{learnerId}");
        afterConfirm.GetProperty("isPremiumActive").GetBoolean().Should().BeTrue();
        afterConfirm.GetProperty("daysUntilExpiry").GetInt32().Should().BeGreaterThan(0);

        // Cancel keeps access until expiry.
        var cancelResponse = await client.PostAsync($"/api/subscription/{learnerId}/cancel", null);
        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await ReadJsonAsync(cancelResponse);
        cancelled.GetProperty("status").GetInt32().Should().Be(3); // Cancelled
        cancelled.GetProperty("isPremiumActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Start_returns_503_when_online_payments_are_disabled()
    {
        // Mirror production's default: online payments OFF (only the free plan is sold). The
        // upgrade must be refused server-side so the dev gateway can't auto-grant Premium.
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<Application.Common.IPaymentAvailability>();
                services.AddSingleton<Application.Common.IPaymentAvailability>(
                    new Infrastructure.Subscription.ConfigPaymentAvailability(paymentsEnabled: false));
            }))
            .CreateClient();
        var learnerId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/subscription/{learnerId}/start", new { plan = 0, provider = 1 });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.ServiceUnavailable);
        var body = await ReadJsonAsync(response);
        body.GetProperty("code").GetString().Should().Be("payments_unavailable");
    }

    [Fact]
    public async Task Confirming_an_unknown_transaction_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/subscription/payments/confirm", new { transactionId = "does-not-exist" });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Dev_checkout_link_GET_confirms_payment_and_redirects_into_app()
    {
        // The browser opens the LocalPaymentGateway checkout URL via a full-page navigation
        // (window.location.href), which is a GET - not the POST webhook. Don't auto-follow so
        // we can assert the redirect that lands the user back in the SPA.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var learnerId = Guid.NewGuid();

        var startResponse = await client.PostAsJsonAsync(
            $"/api/subscription/{learnerId}/start", new { plan = 0, provider = 1 });
        startResponse.EnsureSuccessStatusCode();
        var start = await ReadJsonAsync(startResponse);
        var checkoutUrl = start.GetProperty("checkoutUrl").GetString();
        checkoutUrl.Should().StartWith("/api/subscription/payments/mock-checkout?provider=click&transactionId=");

        // Follow the dev checkout link exactly as the browser would: a GET.
        var confirmResponse = await client.GetAsync(checkoutUrl);
        confirmResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        confirmResponse.Headers.Location!.OriginalString.Should().EndWith("/profile?payment=success");

        // Premium is now active.
        var afterConfirm = await GetJsonAsync(client, $"/api/subscription/{learnerId}");
        afterConfirm.GetProperty("isPremiumActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Dev_checkout_GET_without_transaction_id_returns_400()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/subscription/payments/mock-checkout");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Click_prepare_with_invalid_signature_returns_click_error_contract()
    {
        var client = _factory.CreateClient();
        var form = new Dictionary<string, string>
        {
            ["click_trans_id"] = "1",
            ["service_id"] = "109430",
            ["click_paydoc_id"] = "2",
            ["merchant_trans_id"] = "unknown",
            ["amount"] = "59990.00",
            ["action"] = "0",
            ["error"] = "0",
            ["error_note"] = "Success",
            ["sign_time"] = "2026-08-06 12:00:00",
            ["sign_string"] = "invalid",
        };

        var response = await client.PostAsync(
            "/api/subscription/payments/click/prepare", new FormUrlEncodedContent(form));

        response.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(response);
        body.GetProperty("error").GetInt32().Should().Be(-1);
        body.GetProperty("error_note").GetString().Should().Be("SIGN CHECK FAILED!");
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
