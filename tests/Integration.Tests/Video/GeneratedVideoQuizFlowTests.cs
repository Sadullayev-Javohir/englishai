using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Video.Models;
using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Video;

public class GeneratedVideoQuizFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public GeneratedVideoQuizFlowTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Transcript_generation_resume_grading_and_owner_protection_work_over_HTTP()
    {
        var question = new GeneratedVideoQuestion(Guid.NewGuid(), "Who speaks?", "Kim gapiryapti?",
            ["Tim", "Tom", "Sam", "Joe"], 0, 1, 4, "I'm Tim.", "Men Timman.", "U o'zini Tim deb tanishtirdi.");
        var generator = Substitute.For<IVideoQuizGenerator>();
        generator.GenerateAsync(Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<IReadOnlyList<TranscriptSegment>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { question });
        await using var app = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.RemoveAll<IVideoQuizGenerator>();
            services.AddSingleton(generator);
        }));
        var lesson = VideoLesson.Curate("I_tRSrPru94", "Introductions", "BBC", 150, "Greetings", CefrLevel.A2,
            [TranscriptSegment.Create(1, 4, "I'm Tim.")], [], DateTimeOffset.UtcNow);
        await app.Services.GetRequiredService<IVideoRepository>().SaveAsync(lesson, default);
        var learner = Guid.NewGuid();
        using var client = app.CreateClient();
        var generated = await client.PostAsJsonAsync($"/api/video/{lesson.Id}/quiz", new { learnerId = learner });
        generated.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await generated.Content.ReadFromJsonAsync<JsonElement>();
        var quizId = json.GetProperty("quizId").GetGuid();
        json.GetProperty("questions")[0].TryGetProperty("correctOptionIndex", out _).Should().BeFalse();
        var resume = await client.GetAsync($"/api/video/quiz/{quizId}?learnerId={learner}");
        resume.StatusCode.Should().Be(HttpStatusCode.OK);
        var other = await client.GetAsync($"/api/video/quiz/{quizId}?learnerId={Guid.NewGuid()}");
        other.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var incomplete = await client.PostAsJsonAsync("/api/video/quiz", new {
            videoLessonId = lesson.Id, learnerId = learner, quizId, answers = Array.Empty<object>()
        });
        incomplete.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var graded = await client.PostAsJsonAsync("/api/video/quiz", new {
            videoLessonId = lesson.Id, learnerId = learner, quizId,
            answers = new[] { new { questionId = question.Id, selectedOptionIndex = 0 } }
        });
        graded.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await graded.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("correctCount").GetInt32().Should().Be(1);
        result.GetProperty("outcomes")[0].GetProperty("correctOptionIndex").GetInt32().Should().Be(0);
        result.TryGetProperty("awardedXp", out _).Should().BeTrue();
        var resumed = await client.GetFromJsonAsync<JsonElement>($"/api/video/quiz/{quizId}?learnerId={learner}");
        resumed.GetProperty("result").GetProperty("correctCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Missing_transcript_returns_conflict_without_AI_work()
    {
        var lesson = VideoLesson.Ingest("abcdefghijk", "No captions", "Channel", 10, "Test", [], DateTimeOffset.UtcNow);
        await _factory.Services.GetRequiredService<IVideoRepository>().SaveAsync(lesson, default);
        var response = await _factory.CreateClient().PostAsJsonAsync($"/api/video/{lesson.Id}/quiz", new { learnerId = Guid.NewGuid() });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString().Should().Be("transcript_unavailable");
    }
}
