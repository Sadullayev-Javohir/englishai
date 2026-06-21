using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end test of the vocabulary SRS over HTTP (PROJECT-SPEC Faza 3): learning a
/// word schedules its first review, the word appears in the learner's list, and a
/// passed review advances it along the 3/7/21 ladder. Runs fully in-memory (no DB).
/// </summary>
public class VocabularyFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public VocabularyFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Learn_word_then_review_advances_the_schedule()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        // Learn a word - it starts at the Day3 stage.
        var learnResponse = await client.PostAsJsonAsync("/api/vocabulary/learn", new
        {
            learnerId,
            word = "innovation",
            translation = "yangilik",
            exampleSentence = "An age of innovation.",
            source = 0
        });
        learnResponse.EnsureSuccessStatusCode();
        var learned = await ReadJsonAsync(learnResponse);
        learned.GetProperty("stage").GetInt32().Should().Be(0); // Day3
        var itemId = learned.GetProperty("id").GetGuid();

        // The word shows up in the learner's vocabulary list.
        var list = await GetJsonAsync(client, $"/api/vocabulary/{learnerId}");
        list.GetArrayLength().Should().Be(1);
        list[0].GetProperty("word").GetString().Should().Be("innovation");

        var removeResponse = await client.DeleteAsync($"/api/vocabulary/{learnerId}/{itemId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetJsonAsync(client, $"/api/vocabulary/{learnerId}"))
            .GetArrayLength().Should().Be(0);

        learnResponse = await client.PostAsJsonAsync("/api/vocabulary/learn", new
        {
            learnerId,
            word = "innovation",
            translation = "yangilik",
            exampleSentence = "An age of innovation.",
            source = 0
        });
        learnResponse.EnsureSuccessStatusCode();
        learned = await ReadJsonAsync(learnResponse);
        itemId = learned.GetProperty("id").GetGuid();

        // A freshly learned word starts at the ClozeChoice mini-test (Day3), verified server-side
        // against the item's own word rather than a client-asserted "passed" boolean.
        var reviewResponse = await client.PostAsJsonAsync("/api/vocabulary/review", new
        {
            vocabularyItemId = itemId,
            submittedAnswer = "innovation"
        });
        reviewResponse.EnsureSuccessStatusCode();
        var reviewed = await ReadJsonAsync(reviewResponse);
        reviewed.GetProperty("stage").GetInt32().Should().Be(1); // Day7
        reviewed.GetProperty("mastered").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Record_topic_module_score_then_read_completion_back()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        (await client.PostAsJsonAsync("/api/learning/start", new { learnerId, level = 2 }))
            .EnsureSuccessStatusCode();

        // Take a topic from the learner's seeded A2 catalog.
        var topics = await GetJsonAsync(client, $"/api/vocabulary/topics/{learnerId}?level=2");
        topics.GetArrayLength().Should().BeGreaterThan(0);
        var topicId = topics[0].GetProperty("id").GetGuid();

        // Not started yet: a zeroed six-module checklist.
        var before = await GetJsonAsync(client, $"/api/vocabulary/topic/{topicId}/completion/{learnerId}");
        before.GetProperty("isMastered").GetBoolean().Should().BeFalse();
        before.GetProperty("passedModuleCount").GetInt32().Should().Be(0);
        before.GetProperty("modules").GetArrayLength().Should().Be(6);

        // The first eligible skill is Vocabulary (SkillType.Vocabulary == 6).
        var recordResponse = await client.PostAsJsonAsync("/api/vocabulary/topic/completion", new
        {
            learnerId,
            topicId,
            module = 6,
            score = 85
        });
        recordResponse.EnsureSuccessStatusCode();
        var recorded = await ReadJsonAsync(recordResponse);
        recorded.GetProperty("justMastered").GetBoolean().Should().BeFalse();
        recorded.GetProperty("completion").GetProperty("passedModuleCount").GetInt32().Should().Be(1);

        // Reading back reflects the recorded score.
        var after = await GetJsonAsync(client, $"/api/vocabulary/topic/{topicId}/completion/{learnerId}");
        var vocabulary = after.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Vocabulary");
        vocabulary.GetProperty("score").GetInt32().Should().Be(85);
        vocabulary.GetProperty("passed").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Notifications_endpoint_returns_the_daily_plan_nudge_for_a_new_learner()
    {
        var client = _factory.CreateClient();

        // A brand-new learner has no due words and nothing practiced today, so the feed carries the
        // "start your daily plan" reminder deep-linking to Home. (Past 08:00 local it is joined by the
        // daily "learn English" nudge, so assert on the presence of the plan reminder, not the count.)
        var notifications = await GetJsonAsync(client, $"/api/vocabulary/{Guid.NewGuid()}/notifications");

        var plan = notifications.GetProperty("items").EnumerateArray()
            .Single(n => n.GetProperty("code").GetString() == "notify.daily_plan");
        plan.GetProperty("linkUrl").GetString().Should().Be("/home");
        plan.GetProperty("isRead").GetBoolean().Should().BeFalse();
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
