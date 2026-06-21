using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end test of the Video/Listening module over HTTP (PROJECT-SPEC Faza 4): the
/// adaptive catalog returns curated lessons, a lesson exposes its interactive transcript
/// and (answerless) quiz, the quiz is graded server-side, and a word saved from a video
/// flows into the SRS as a Video-sourced item (Faza 4 ↔ Faza 3). Runs fully in-memory.
/// </summary>
public class VideoFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public VideoFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_then_lesson_then_quiz_grades_server_side()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        // The adaptive catalog returns verified-live lessons near the (default A2) level.
        var catalog = await GetJsonAsync(client, $"/api/video/catalog/{learnerId}");
        catalog.GetArrayLength().Should().BeGreaterThan(0);

        // The seeded catalog deliberately ships no curated transcript/quiz (those arrive from
        // real captions in the captions phase). To exercise the transcript+quiz flow, save a
        // fully curated lesson into the repository, then drive the endpoints against it.
        var lessonId = await SeedCuratedLessonAsync();

        // The lesson detail carries the transcript and quiz, with answers withheld.
        var lesson = await GetJsonAsync(client, $"/api/video/{lessonId}");
        lesson.GetProperty("transcript").GetArrayLength().Should().BeGreaterThan(0);
        var questions = lesson.GetProperty("questions");
        questions.GetArrayLength().Should().BeGreaterThan(0);
        questions[0].TryGetProperty("correctOptionIndex", out _).Should().BeFalse();
        var questionId = questions[0].GetProperty("id").GetGuid();

        // Submitting an answer returns a graded result (correct answer now revealed).
        var quizResponse = await client.PostAsJsonAsync("/api/video/quiz", new
        {
            videoLessonId = lessonId,
            learnerId,
            answers = new[] { new { questionId, selectedOptionIndex = 0 } }
        });
        quizResponse.EnsureSuccessStatusCode();
        var result = await ReadJsonAsync(quizResponse);
        result.GetProperty("totalQuestions").GetInt32().Should().BeGreaterThan(0);
        result.GetProperty("outcomes").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Rating_a_video_difficulty_is_accepted()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        var catalog = await GetJsonAsync(client, $"/api/video/catalog/{learnerId}");
        var lessonId = catalog[0].GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync("/api/video/rate", new
        {
            learnerId,
            videoLessonId = lessonId,
            rating = 2 // TooHard
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Saving_a_word_from_a_video_lands_in_the_SRS_as_a_video_word()
    {
        var client = _factory.CreateClient();
        var learnerId = Guid.NewGuid();

        var saveResponse = await client.PostAsJsonAsync("/api/video/word", new
        {
            learnerId,
            word = "consistency",
            translation = "doimiylik",
            exampleSentence = "Consistency is the key to success."
        });
        saveResponse.EnsureSuccessStatusCode();
        var saved = await ReadJsonAsync(saveResponse);
        saved.GetProperty("source").GetInt32().Should().Be(2); // VocabularySource.Video

        // The word appears in the learner's vocabulary list.
        var list = await GetJsonAsync(client, $"/api/vocabulary/{learnerId}");
        list.GetArrayLength().Should().Be(1);
        list[0].GetProperty("word").GetString().Should().Be("consistency");
        list[0].GetProperty("source").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Unknown_lesson_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/video/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Saves a fully curated A2 lesson (transcript + quiz) into the in-memory repository so
    /// the transcript/quiz endpoints have content to serve, independent of the seed catalog.
    /// </summary>
    private async Task<Guid> SeedCuratedLessonAsync()
    {
        var repository = _factory.Services.GetRequiredService<IVideoRepository>();
        var lesson = VideoLesson.Curate(
            "dQw4w9WgXcQ", "Test Lesson", "Test Channel", 60, "everyday", CefrLevel.A2,
            new[]
            {
                TranscriptSegment.Create(0, 4, "Every morning I wake up at seven o'clock.", "Har kuni ertalab soat yettida uyg'onaman."),
            },
            new[]
            {
                ComprehensionQuestion.Create(
                    "What time does the speaker wake up?",
                    new[] { "Six o'clock", "Seven o'clock", "Eight o'clock" }, 1, "hint.daily_time"),
            },
            DateTimeOffset.UtcNow);

        await repository.SaveAsync(lesson, CancellationToken.None);
        return lesson.Id;
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
