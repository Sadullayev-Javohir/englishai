using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end test of the learner model over HTTP: finishing a placement test creates
/// the learner profile, then activity is recorded and the overview / recommendations /
/// growth endpoints reflect it. Runs fully in-memory (no database required).
/// </summary>
public class LearningFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public LearningFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Placement_creates_profile_then_activity_flows_into_analytics()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        await CompletePlacementAsync(client, learnerId);

        // Profile now exists: overview returns six skill scores.
        var overview = await GetJsonAsync(client, $"/api/learning/{learnerId}/overview");
        overview.GetProperty("skillScores").GetArrayLength().Should().Be(6);

        // Record a strong Speaking result with an article error.
        var activityResponse = await client.PostAsJsonAsync("/api/learning/activity", new
        {
            learnerId,
            skill = 1, // Speaking
            score = 88,
            errors = new[] { 1 } // Articles
        });
        activityResponse.EnsureSuccessStatusCode();

        var afterActivity = await ReadJsonAsync(activityResponse);
        afterActivity.GetProperty("errorHeatmap").GetArrayLength().Should().BeGreaterThan(0);

        // Recommendations come back with resolved Uzbek text.
        var recommendations = await GetJsonAsync(client, $"/api/learning/{learnerId}/recommendations");
        recommendations.GetArrayLength().Should().BeGreaterThan(0);
        recommendations[0].GetProperty("text").GetString().Should().NotBeNullOrWhiteSpace();

        // Growth series returns the requested number of weeks (4 * 6 skills).
        var growth = await GetJsonAsync(client, $"/api/learning/{learnerId}/growth?weeks=4");
        growth.GetArrayLength().Should().Be(4 * 6);
    }

    [Fact]
    public async Task Overview_for_unknown_learner_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/learning/{Guid.NewGuid()}/overview");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task CompletePlacementAsync(HttpClient client, Guid learnerId)
    {
        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start", new { learnerId, includeSpeaking = false });
        startResponse.EnsureSuccessStatusCode();

        var start = await ReadJsonAsync(startResponse);
        var sessionId = start.GetProperty("sessionId").GetGuid();

        await PlacementDriver.RunToCompletionAsync(client, sessionId, start.GetProperty("firstItem"));

        var finalizeResponse = await client.PostAsJsonAsync("/api/placement/finalize", new { sessionId });
        finalizeResponse.EnsureSuccessStatusCode();
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
