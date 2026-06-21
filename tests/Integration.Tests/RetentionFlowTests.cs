using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end tests of the retention/growth HTTP surface (PROJECT-SPEC Qism I): churn risk,
/// feature-flag assignments and cohort metrics. Runs on in-memory adapters (no DB/Redis),
/// with the feature flags served from the seeded in-memory repository.
/// </summary>
public class RetentionFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RetentionFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task An_unknown_learner_is_flagged_as_having_abandoned_onboarding()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        var churn = await GetJsonAsync(client, $"/api/retention/{learnerId}/churn");

        churn.GetProperty("isAtRisk").GetBoolean().Should().BeTrue();
        churn.GetProperty("signals").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Feature_flags_return_a_stable_assigned_variant()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        var flags = await GetJsonAsync(client, $"/api/retention/{learnerId}/flags");
        flags.GetArrayLength().Should().BeGreaterThan(0);

        var single = await GetJsonAsync(client, $"/api/retention/{learnerId}/flags/daily_goal_size");
        var firstRead = single.GetProperty("variant").GetString();

        // Same learner, same flag → same variant every time (deterministic assignment).
        var second = await GetJsonAsync(client, $"/api/retention/{learnerId}/flags/daily_goal_size");
        second.GetProperty("variant").GetString().Should().Be(firstRead);
        single.GetProperty("value").GetString().Should().BeOneOf("3", "5");
    }

    [Fact]
    public async Task An_unknown_flag_key_is_404()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/retention/{learnerId}/flags/does_not_exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Retention_metrics_endpoint_returns_the_cohort_horizons()
    {
        var client = _factory.CreateClient();

        var metrics = await GetJsonAsync(client, "/api/retention/metrics");

        metrics.GetProperty("cohortSize").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        metrics.GetProperty("d1").GetProperty("day").GetInt32().Should().Be(1);
        metrics.GetProperty("d7").GetProperty("day").GetInt32().Should().Be(7);
        metrics.GetProperty("d30").GetProperty("day").GetInt32().Should().Be(30);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
