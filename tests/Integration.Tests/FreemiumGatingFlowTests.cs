using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end freemium gating over HTTP (PROJECT-SPEC H.1): a free learner gets a bounded number of
/// speaking sessions per day (402 beyond it); upgrading to Premium lifts the limit. Runs on the
/// in-memory adapters and Local payment gateway.
/// </summary>
public class FreemiumGatingFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private const int SpeakingSessionFeature = 0; // PremiumFeature.SpeakingSession

    private readonly TestWebApplicationFactory _factory;

    public FreemiumGatingFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Free_learner_is_capped_at_the_daily_speaking_session_count()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();
        var dailySessions = EntitlementPolicy.FreeLimitFor(PremiumFeature.SpeakingSession).MaxUses;

        // Access check before any usage: allowed, the full day's sessions remaining.
        var access = await GetJsonAsync(client, $"/api/subscription/{learnerId}/access/{SpeakingSessionFeature}");
        access.GetProperty("isAllowed").GetBoolean().Should().BeTrue();
        access.GetProperty("remaining").GetInt32().Should().Be(dailySessions);

        // Every session inside the day's allowance works. (The real cost guard is the daily speaking
        // MINUTE budget; this count only stops a learner opening unbounded rooms.)
        for (var used = 0; used < dailySessions; used++)
        {
            var response = await client.PostAsJsonAsync("/api/speaking/start", new { learnerId, level = 2 });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // The next one is blocked with 402 Payment Required.
        var blocked = await client.PostAsJsonAsync("/api/speaking/start", new { learnerId, level = 2 });
        blocked.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);

        // Access check now reports denied.
        var afterAccess = await GetJsonAsync(client, $"/api/subscription/{learnerId}/access/{SpeakingSessionFeature}");
        afterAccess.GetProperty("isAllowed").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Premium_learner_has_no_speaking_limit()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        await ActivatePremiumAsync(client, learnerId);

        // Multiple sessions all succeed.
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/speaking/start", new { learnerId, level = 2 });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var access = await GetJsonAsync(client, $"/api/subscription/{learnerId}/access/{SpeakingSessionFeature}");
        access.GetProperty("isAllowed").GetBoolean().Should().BeTrue();
        access.GetProperty("limit").GetInt32().Should().Be(-1); // unlimited
    }

    private static async Task ActivatePremiumAsync(HttpClient client, Guid learnerId)
    {
        var startResponse = await client.PostAsJsonAsync(
            $"/api/subscription/{learnerId}/start", new { plan = 0 });
        startResponse.EnsureSuccessStatusCode();
        var start = await ReadJsonAsync(startResponse);
        var transactionId = start.GetProperty("transactionId").GetString();

        var confirmResponse = await client.PostAsJsonAsync(
            "/api/subscription/payments/confirm", new { transactionId });
        confirmResponse.EnsureSuccessStatusCode();
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
