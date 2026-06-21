using System.Net;
using System.Net.Http.Json;
using Application.Gamification;
using Application.Gamification.Dtos;
using Domain.Learning;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// The P1 failure boundary, end to end over HTTP.
///
/// The learner's score lives in PostgreSQL; the streak, XP and daily-goal updates that follow live
/// in Redis. Before this, a Redis blip meant a learner who had just finished a module received an
/// HTTP 500 and a "try again" - and re-submitted work that had already been committed. Losing a
/// streak tick is the far smaller harm, so the reward is best-effort and the request still succeeds.
/// </summary>
public class LearningRewardFailureFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public LearningRewardFailureFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Recording_a_module_score_succeeds_even_when_the_reward_store_is_down()
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDailyProgressRecorder>();
                services.AddSingleton<IDailyProgressRecorder>(new UnavailableDailyProgressRecorder());
            }))
            .CreateClient();

        var learnerId = Guid.NewGuid();
        // Place the learner first: topics are listed relative to their level.
        (await client.PostAsJsonAsync("/api/learning/start", new { learnerId, level = 2 }))
            .EnsureSuccessStatusCode();
        var topicId = await FirstTopicIdAsync(client, learnerId);

        var response = await client.PostAsJsonAsync("/api/vocabulary/topic/completion", new
        {
            learnerId,
            topicId,
            module = (int)SkillType.Vocabulary,
            score = 90,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "a graded module must not be lost to a reward outage");

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("completion").GetProperty("modules").EnumerateArray()
            .Single(module => module.GetProperty("module").GetString() == "Vocabulary")
            .GetProperty("score").GetInt32()
            .Should().Be(90, "the score is committed before the reward is attempted");
    }

    private static async Task<Guid> FirstTopicIdAsync(HttpClient client, Guid learnerId)
    {
        var response = await client.GetAsync($"/api/vocabulary/topics/{learnerId}?level=2");
        response.EnsureSuccessStatusCode();
        var topics = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return topics.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    /// <summary>Stands in for a Redis outage on every reward path.</summary>
    private sealed class UnavailableDailyProgressRecorder : IDailyProgressRecorder
    {
        public Task<SkillRewardDto> RecordSkillAsync(
            Guid learnerId, SkillType skill, int score, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("gamification store unavailable");
    }
}
