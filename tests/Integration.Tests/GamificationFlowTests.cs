using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end test of the daily-goal/streak gamification over HTTP (PROJECT-SPEC Faza 5).
/// Runs on the in-memory gamification store (no Redis required).
/// </summary>
public class GamificationFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GamificationFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Completing_the_daily_goal_starts_a_streak()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        // Fresh learner: status is empty.
        var initial = await GetJsonAsync(client, $"/api/gamification/{learnerId}");
        initial.GetProperty("currentStreak").GetInt32().Should().Be(0);
        initial.GetProperty("isGoalMet").GetBoolean().Should().BeFalse();
        var target = initial.GetProperty("dailyGoalTarget").GetInt32();
        target.Should().BeGreaterThan(0);

        // Complete enough tasks to meet the goal.
        JsonElement last = default;
        for (var i = 0; i < target; i++)
        {
            var response = await client.PostAsync($"/api/gamification/{learnerId}/complete-task", null);
            response.EnsureSuccessStatusCode();
            last = await ReadJsonAsync(response);
        }

        last.GetProperty("isGoalMet").GetBoolean().Should().BeTrue();
        last.GetProperty("currentStreak").GetInt32().Should().Be(1);
        last.GetProperty("isStreakAtRisk").GetBoolean().Should().BeFalse();

        // A follow-up status read reflects the same state.
        var afterStatus = await GetJsonAsync(client, $"/api/gamification/{learnerId}");
        afterStatus.GetProperty("todayCompletedTasks").GetInt32().Should().Be(target);
        afterStatus.GetProperty("currentStreak").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Completing_a_topic_module_awards_points_and_updates_the_leaderboard()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        // Start at A2 and complete the first eligible skill (Vocabulary) through the same HTTP path
        // a real module-completion flow drives (RecordTopicModuleScore -> DailyProgressRecorder ->
        // PointsService), proving the leaderboard/points wiring end to end over real HTTP.
        (await client.PostAsJsonAsync("/api/learning/start", new { learnerId, level = 2 }))
            .EnsureSuccessStatusCode();
        var topics = await GetJsonAsync(client, $"/api/vocabulary/topics/{learnerId}?level=2");
        var topicId = topics[0].GetProperty("id").GetGuid();

        var recordResponse = await client.PostAsJsonAsync("/api/vocabulary/topic/completion", new
        {
            learnerId,
            topicId,
            module = 6, // SkillType.Vocabulary
            score = 85
        });
        recordResponse.EnsureSuccessStatusCode();

        // Points: an 85% score earns 15 XP + first-activity-of-day bonus (5) = 20.
        var points = await GetJsonAsync(client, $"/api/gamification/{learnerId}/points");
        points.GetProperty("lifetimeXp").GetInt32().Should().Be(20);
        points.GetProperty("spendableCoins").GetInt32().Should().Be(20);
        var tiers = points.GetProperty("tiers").EnumerateArray().ToList();
        tiers.Should().OnlyContain(t => !t.GetProperty("canAfford").GetBoolean());

        // Leaderboard: a learner with no placement profile defaults to the A2 board, and shows
        // up there with the same score just earned.
        var leaderboard = await GetJsonAsync(client, $"/api/gamification/{learnerId}/leaderboard");
        leaderboard.GetProperty("level").GetInt32().Should().Be(2); // CefrLevel.A2
        var mine = leaderboard.GetProperty("top").EnumerateArray()
            .SingleOrDefault(e => e.GetProperty("learnerId").GetGuid() == learnerId);
        mine.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        mine.GetProperty("score").GetInt32().Should().Be(20);
        mine.GetProperty("isCurrentUser").GetBoolean().Should().BeTrue();

        // Redeeming a tier the learner can't afford yet is rejected (409), proving the domain
        // rule and the error-middleware mapping both work over the real HTTP pipeline.
        var redeemResponse = await client.PostAsJsonAsync(
            $"/api/gamification/{learnerId}/redeem-discount", new { coinsCost = 4_000 });
        redeemResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
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
