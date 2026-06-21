using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

public class ListeningFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ListeningFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Topic_hides_answer_and_check_endpoint_returns_immediate_feedback()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();
        var catalog = await GetJsonAsync(client, $"/api/listening/catalog/{learnerId}");
        var topicId = catalog[0].GetProperty("topicId").GetGuid();

        var exercise = await GetJsonAsync(client, $"/api/listening/topic/{topicId}");
        var question = exercise.GetProperty("questions")[0];
        question.TryGetProperty("correctOptionIndex", out _).Should().BeFalse();
        var questionId = question.GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync("/api/listening/answers/check", new
        {
            topicId,
            questionId,
            selectedOptionIndex = 0,
        });

        response.EnsureSuccessStatusCode();
        var feedback = await ReadJsonAsync(response);
        feedback.GetProperty("questionId").GetGuid().Should().Be(questionId);
        feedback.GetProperty("correctOptionIndex").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        feedback.TryGetProperty("isCorrect", out _).Should().BeTrue();
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
