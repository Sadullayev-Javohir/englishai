using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests.Writing;

/// <summary>
/// End-to-end test of the Writing module over HTTP (PROJECT-SPEC G.3, topic-scoped): the catalog
/// returns the learning-spine topics for the learner's level, opening a topic generates and caches
/// its writing task (prompt + guidance + word range), and a submission is assessed across the four
/// dimensions with vetted Uzbek issue explanations and credits the topic's Writing module (K.5).
/// Runs fully in-memory with the deterministic Local generator + assessor (no LLM key).
/// </summary>
public class WritingFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public WritingFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_then_topic_then_submit_is_assessed_and_credits_the_topic()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();
        (await client.PostAsJsonAsync("/api/learning/start", new { learnerId, level = 2 }))
            .EnsureSuccessStatusCode();

        // The catalog is the 50 learning-spine topics for the learner's level.
        var catalog = await GetJsonAsync(client, $"/api/writing/catalog/{learnerId}");
        catalog.GetArrayLength().Should().BeGreaterThan(0);
        var topicId = catalog[0].GetProperty("topicId").GetGuid();
        await PassModuleAsync(client, learnerId, topicId, module: 6); // Vocabulary
        await PassModuleAsync(client, learnerId, topicId, module: 5); // Grammar
        await PassModuleAsync(client, learnerId, topicId, module: 3); // Reading

        // Opening a topic generates + caches its writing task.
        var task = await GetJsonAsync(client, $"/api/writing/topic/{topicId}");
        task.GetProperty("isReady").GetBoolean().Should().BeTrue();
        task.GetProperty("prompt").GetString().Should().NotBeNullOrEmpty();
        task.GetProperty("minWords").GetInt32().Should().BeGreaterThan(0);
        task.GetProperty("guidance").GetArrayLength().Should().BeGreaterThan(0);

        var response = await client.PostAsJsonAsync("/api/writing/submit", new
        {
            topicId,
            learnerId,
            text = "I went to the city centre last weekend. It was a busy place with many shops and cafes."
        });
        response.EnsureSuccessStatusCode();
        var result = await ReadJsonAsync(response);

        result.GetProperty("dimensionScores").GetArrayLength().Should().Be(4);
        result.GetProperty("overallPercent").GetInt32().Should().BeInRange(0, 100);
        result.TryGetProperty("estimatedLevel", out _).Should().BeTrue();
        result.GetProperty("assessmentSource").GetInt32().Should().Be(1);

        // Every submission is topic-scoped, so the Writing module is always credited (K.5).
        var completion = result.GetProperty("completion");
        completion.ValueKind.Should().Be(JsonValueKind.Object);
        completion.GetProperty("topicId").GetGuid().Should().Be(topicId);
        var writingModule = completion.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Writing");
        writingModule.GetProperty("score").GetInt32().Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task Unknown_topic_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/writing/topic/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task PassModuleAsync(HttpClient client, Guid learnerId, Guid topicId, int module)
    {
        var response = await client.PostAsJsonAsync("/api/vocabulary/topic/completion", new
        {
            learnerId,
            topicId,
            module,
            score = 100,
        });
        response.EnsureSuccessStatusCode();
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
