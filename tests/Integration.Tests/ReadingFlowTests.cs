using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end test of the Reading module over HTTP (PROJECT-SPEC Faza 6): the catalog returns the
/// learning-spine topics for the learner's level, opening a topic lazily generates and caches its
/// reading lesson (body, interactive glossary and answerless quiz), the quiz is graded server-side
/// and credits the topic's Reading module, and a glossed word saved from a passage flows into the
/// SRS as a Reading-sourced item (Faza 6 ↔ Faza 3). Runs fully in-memory (Local generator).
/// </summary>
public class ReadingFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ReadingFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_then_topic_then_quiz_grades_server_side_and_credits_topic()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();
        (await client.PostAsJsonAsync("/api/learning/start", new { learnerId, level = 2 }))
            .EnsureSuccessStatusCode();

        // The learner explicitly starts at A2, whose catalog contains 50 spine topics.
        var catalog = await GetJsonAsync(client, $"/api/reading/catalog/{learnerId}");
        catalog.GetArrayLength().Should().Be(50);
        var topicId = catalog[0].GetProperty("topicId").GetGuid();
        await PassModuleAsync(client, learnerId, topicId, module: 6); // Vocabulary
        await PassModuleAsync(client, learnerId, topicId, module: 5); // Grammar

        // Opening the topic lazily generates and caches its reading lesson.
        var passage = await GetJsonAsync(client, $"/api/reading/topic/{topicId}");
        passage.GetProperty("isReady").GetBoolean().Should().BeTrue();
        passage.GetProperty("topicId").GetGuid().Should().Be(topicId);
        passage.GetProperty("body").GetString().Should().NotBeNullOrEmpty();
        passage.GetProperty("glossary").GetArrayLength().Should().BeGreaterThan(0);
        var questions = passage.GetProperty("questions");
        questions.GetArrayLength().Should().BeGreaterThan(0);
        questions[0].TryGetProperty("correctOptionIndex", out _).Should().BeFalse();
        var questionId = questions[0].GetProperty("id").GetGuid();

        // Immediate feedback uses a side-effect-free endpoint while the passage DTO keeps the key hidden.
        var checkResponse = await client.PostAsJsonAsync("/api/reading/quiz/check", new
        {
            topicId,
            questionId,
            selectedOptionIndex = 0
        });
        checkResponse.EnsureSuccessStatusCode();
        var check = await ReadJsonAsync(checkResponse);
        check.GetProperty("questionId").GetGuid().Should().Be(questionId);
        check.GetProperty("correctOptionIndex").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        check.TryGetProperty("isCorrect", out _).Should().BeTrue();

        var quizResponse = await client.PostAsJsonAsync("/api/reading/quiz", new
        {
            topicId,
            learnerId,
            answers = new[] { new { questionId, selectedOptionIndex = 0 } }
        });
        quizResponse.EnsureSuccessStatusCode();
        var result = await ReadJsonAsync(quizResponse);
        result.GetProperty("totalQuestions").GetInt32().Should().BeGreaterThan(0);
        result.GetProperty("outcomes").GetArrayLength().Should().BeGreaterThan(0);

        // The score is credited toward the topic's six-module mastery checklist (K.5).
        result.GetProperty("completion").GetProperty("modules").GetArrayLength().Should().Be(6);
    }

    [Fact]
    public async Task Saving_a_word_from_a_passage_lands_in_the_SRS_as_a_reading_word()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        var saveResponse = await client.PostAsJsonAsync("/api/reading/word", new
        {
            learnerId,
            word = "essential",
            translation = "zarur",
            exampleSentence = "Sleep is essential for the body."
        });
        saveResponse.EnsureSuccessStatusCode();
        var saved = await ReadJsonAsync(saveResponse);
        saved.GetProperty("source").GetInt32().Should().Be(3); // VocabularySource.Reading

        var list = await GetJsonAsync(client, $"/api/vocabulary/{learnerId}");
        list.GetArrayLength().Should().Be(1);
        list[0].GetProperty("word").GetString().Should().Be("essential");
        list[0].GetProperty("source").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Unknown_topic_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/reading/topic/{Guid.NewGuid()}");

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
