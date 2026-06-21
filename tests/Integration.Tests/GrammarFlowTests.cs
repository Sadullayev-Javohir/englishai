using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end test of the Grammar module over HTTP (PROJECT-SPEC G.2 - topic-scoped 5-step lessons):
/// the catalog returns the learning-spine topics for the learner's level, opening a topic lazily
/// generates and caches its grammar lesson (context intro, English rule, answerless exercises and
/// application tasks), and the exercises are graded server-side and credit the topic's Grammar
/// module (K.5). Runs fully in-memory (Local generator).
/// </summary>
public class GrammarFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GrammarFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_then_topic_then_exercises_grade_server_side_and_credit_topic()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();
        (await client.PostAsJsonAsync("/api/learning/start", new { learnerId, level = 2 }))
            .EnsureSuccessStatusCode();

        // The learner explicitly starts at A2, whose catalog contains 50 spine topics.
        var catalog = await GetJsonAsync(client, $"/api/grammar/catalog/{learnerId}");
        catalog.GetArrayLength().Should().Be(50);
        var topicId = catalog[0].GetProperty("topicId").GetGuid();
        catalog[0].GetProperty("grammarFocusCode").GetString().Should().NotBeNullOrEmpty();
        await PassModuleAsync(client, learnerId, topicId, module: 6); // Vocabulary

        // Opening the topic lazily generates and caches its grammar lesson.
        var lesson = await GetJsonAsync(client, $"/api/grammar/topic/{topicId}");
        lesson.GetProperty("isReady").GetBoolean().Should().BeTrue();
        lesson.GetProperty("topicId").GetGuid().Should().Be(topicId);
        lesson.GetProperty("contextIntro").GetString().Should().NotBeNullOrEmpty();
        lesson.GetProperty("explanation").GetString().Should().NotBeNullOrEmpty();
        lesson.GetProperty("applicationTasks").GetArrayLength().Should().BeGreaterThan(0);
        var exercises = lesson.GetProperty("exercises");
        exercises.GetArrayLength().Should().BeGreaterThan(0);
        exercises[0].TryGetProperty("correctOptionIndex", out _).Should().BeFalse();
        var exerciseId = exercises[0].GetProperty("id").GetGuid();

        var checkResponse = await client.PostAsJsonAsync("/api/grammar/exercises/check", new
        {
            topicId,
            exerciseId,
            selectedOptionIndex = 0,
        });
        checkResponse.EnsureSuccessStatusCode();
        var check = await ReadJsonAsync(checkResponse);
        check.GetProperty("exerciseId").GetGuid().Should().Be(exerciseId);
        check.GetProperty("correctOptionIndex").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        check.TryGetProperty("isCorrect", out _).Should().BeTrue();

        var response = await client.PostAsJsonAsync("/api/grammar/exercises", new
        {
            topicId,
            learnerId,
            answers = new[] { new { exerciseId, selectedOptionIndex = 0 } }
        });
        response.EnsureSuccessStatusCode();
        var result = await ReadJsonAsync(response);
        result.GetProperty("totalExercises").GetInt32().Should().BeGreaterThan(0);
        result.GetProperty("outcomes").GetArrayLength().Should().BeGreaterThan(0);

        // The score is credited toward the topic's six-module mastery checklist (K.5).
        var completion = result.GetProperty("completion");
        completion.GetProperty("topicId").GetGuid().Should().Be(topicId);
        completion.GetProperty("modules").GetArrayLength().Should().Be(6);
        var grammar = completion.GetProperty("modules")
            .EnumerateArray().Single(m => m.GetProperty("module").GetString() == "Grammar");
        grammar.GetProperty("score").GetInt32().Should().Be(result.GetProperty("scorePercent").GetInt32());
    }

    [Fact]
    public async Task Unknown_topic_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/grammar/topic/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Typed_answers_are_checked_and_regraded_over_http()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();
        (await client.PostAsJsonAsync("/api/learning/start", new { learnerId, level = 2 })).EnsureSuccessStatusCode();
        var catalog = await GetJsonAsync(client, $"/api/grammar/catalog/{learnerId}");
        var topicId = catalog[0].GetProperty("topicId").GetGuid();
        await PassModuleAsync(client, learnerId, topicId, 6);
        var lesson = await GetJsonAsync(client, $"/api/grammar/topic/{topicId}");
        lesson.GetProperty("grammarFocusCode").GetString().Should().NotBeNullOrEmpty();
        var exercise = lesson.GetProperty("exercises")[0];
        var exerciseId = exercise.GetProperty("id").GetGuid();
        var known = await client.PostAsJsonAsync("/api/grammar/exercises/check", new { topicId, exerciseId, selectedOptionIndex = 0 });
        var correctIndex = (await ReadJsonAsync(known)).GetProperty("correctOptionIndex").GetInt32();
        var correctText = exercise.GetProperty("options")[correctIndex].GetString()!;
        var correct = await client.PostAsJsonAsync("/api/grammar/exercises/check", new { topicId, exerciseId, selectedOptionIndex = -1, textAnswer = correctText });
        correct.EnsureSuccessStatusCode();
        (await ReadJsonAsync(correct)).GetProperty("isCorrect").GetBoolean().Should().BeTrue();
        var wrong = await client.PostAsJsonAsync("/api/grammar/exercises/check", new { topicId, exerciseId, selectedOptionIndex = correctIndex, textAnswer = "unknown typed answer" });
        wrong.EnsureSuccessStatusCode();
        (await ReadJsonAsync(wrong)).GetProperty("isCorrect").GetBoolean().Should().BeFalse();
        var empty = await client.PostAsJsonAsync("/api/grammar/exercises/check", new { topicId, exerciseId, selectedOptionIndex = -1, textAnswer = " " });
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var submitted = await client.PostAsJsonAsync("/api/grammar/exercises", new {
            topicId, learnerId, answers = new[] { new { exerciseId, selectedOptionIndex = correctIndex, textAnswer = "unknown typed answer" } }
        });
        submitted.EnsureSuccessStatusCode();
        var graded = await ReadJsonAsync(submitted);
        graded.GetProperty("correctCount").GetInt32().Should().Be(0);
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
