using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Assessment;
using FluentAssertions;
using Infrastructure.Assessment;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// End-to-end test of the placement test over real HTTP: start -> answer loop ->
/// finalize, exercising the endpoints, MediatR pipeline (including validation),
/// and the in-memory adapters. No external services required.
/// </summary>
public class PlacementFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PlacementFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Full_placement_flow_completes_and_returns_a_result()
    {
        var client = _factory.CreateClient();

        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start",
            new { learnerId = Guid.NewGuid(), includeSpeaking = false });
        startResponse.EnsureSuccessStatusCode();

        var start = await ReadJsonAsync(startResponse);
        var sessionId = start.GetProperty("sessionId").GetGuid();

        // Walks every stage including the Writing productive task (speaking excluded).
        await PlacementDriver.RunToCompletionAsync(client, sessionId, start.GetProperty("firstItem"));

        var finalizeResponse = await client.PostAsJsonAsync(
            "/api/placement/finalize",
            new { sessionId });
        finalizeResponse.EnsureSuccessStatusCode();

        var result = await ReadJsonAsync(finalizeResponse);
        result.GetProperty("overallLevel").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        result.GetProperty("stageResults").GetArrayLength().Should().Be(5);
        result.GetProperty("stageResults").EnumerateArray()
            .Select(stage => stage.GetProperty("stage").GetInt32())
            .Should().BeEquivalentTo(new[]
            {
                (int)TestStage.Vocabulary,
                (int)TestStage.Grammar,
                (int)TestStage.Listening,
                (int)TestStage.Reading,
                (int)TestStage.Writing,
            });
    }

    [Fact]
    public async Task Full_six_skill_placement_returns_six_independent_results()
    {
        var client = _factory.CreateClient();
        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start",
            new { learnerId = Guid.NewGuid(), includeSpeaking = true });
        startResponse.EnsureSuccessStatusCode();

        var start = await ReadJsonAsync(startResponse);
        var sessionId = start.GetProperty("sessionId").GetGuid();
        var firstItem = start.GetProperty("firstItem");
        firstItem.GetProperty("totalItems").GetInt32().Should().Be(23);
        firstItem.GetProperty("stageCount").GetInt32().Should().Be(6);

        await PlacementDriver.RunToCompletionAsync(client, sessionId, firstItem);
        var finalizeResponse = await client.PostAsJsonAsync("/api/placement/finalize", new { sessionId });
        finalizeResponse.EnsureSuccessStatusCode();

        var result = await ReadJsonAsync(finalizeResponse);
        result.GetProperty("stageResults").GetArrayLength().Should().Be(6);
        result.GetProperty("stageResults").EnumerateArray()
            .Select(stage => stage.GetProperty("stage").GetInt32())
            .Should().BeEquivalentTo(Enum.GetValues<TestStage>().Select(stage => (int)stage));
    }

    [Fact]
    public async Task Placement_session_can_resume_the_exact_current_item()
    {
        var client = _factory.CreateClient();
        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start",
            new { learnerId = Guid.NewGuid(), includeSpeaking = true });
        startResponse.EnsureSuccessStatusCode();
        var start = await ReadJsonAsync(startResponse);
        var sessionId = start.GetProperty("sessionId").GetGuid();
        var firstItemId = start.GetProperty("firstItem").GetProperty("id").GetGuid();

        var resumeResponse = await client.GetAsync($"/api/placement/session/{sessionId}");
        resumeResponse.EnsureSuccessStatusCode();
        var resumed = await ReadJsonAsync(resumeResponse);

        resumed.GetProperty("isCompleted").GetBoolean().Should().BeFalse();
        resumed.GetProperty("currentItem").GetProperty("id").GetGuid().Should().Be(firstItemId);
    }

    [Fact]
    public async Task Placement_rejects_an_item_that_was_not_served()
    {
        var client = _factory.CreateClient();
        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start",
            new { learnerId = Guid.NewGuid(), includeSpeaking = true });
        startResponse.EnsureSuccessStatusCode();
        var start = await ReadJsonAsync(startResponse);
        var sessionId = start.GetProperty("sessionId").GetGuid();
        var anotherVocabularyQuestion = QuestionBankSeed.Questions
            .First(question => question.Stage == TestStage.Vocabulary &&
                question.Id != start.GetProperty("firstItem").GetProperty("id").GetGuid());

        var response = await client.PostAsJsonAsync(
            "/api/placement/answer",
            new { sessionId, questionId = anotherVocabularyQuestion.Id, selectedOptionIndex = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Placement_rejects_premature_finalize()
    {
        var client = _factory.CreateClient();
        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start",
            new { learnerId = Guid.NewGuid(), includeSpeaking = true });
        startResponse.EnsureSuccessStatusCode();
        var start = await ReadJsonAsync(startResponse);

        var response = await client.PostAsJsonAsync(
            "/api/placement/finalize",
            new { sessionId = start.GetProperty("sessionId").GetGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_A2_only_learner_is_not_over_placed_to_B2_or_higher()
    {
        // Regression for the reported bug: a learner who reliably handles A1/A2 items
        // but fails everything harder used to be rated B2 because one lucky guess on a
        // hard item set the level. With the guessing-aware estimator they must land at
        // A1/A2 (B1 tolerated at the boundary), never B2+.
        var client = _factory.CreateClient();

        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start",
            new { learnerId = Guid.NewGuid(), includeSpeaking = true });
        startResponse.EnsureSuccessStatusCode();

        var start = await ReadJsonAsync(startResponse);
        var sessionId = start.GetProperty("sessionId").GetGuid();

        // Multiple-choice items answered as an A2 learner (correct on A1/A2, wrong above);
        // the Writing/Speaking productive tasks get the driver's deliberately weak default
        // answers, so nothing inflates the result.
        await PlacementDriver.RunToCompletionAsync(
            client, sessionId, start.GetProperty("firstItem"),
            mcqChoice: item => AnswerAsA2Learner(sessionId, item.GetProperty("id").GetGuid()));

        var finalizeResponse = await client.PostAsJsonAsync("/api/placement/finalize", new { sessionId });
        finalizeResponse.EnsureSuccessStatusCode();

        var result = await ReadJsonAsync(finalizeResponse);
        result.GetProperty("overallLevel").GetInt32()
            .Should().BeLessThanOrEqualTo((int)CefrLevel.B1, "an A2-level learner must not be placed at B2 or above");
    }

    // Picks the display index a learner would click: correct on A1/A2 items, wrong on
    // anything harder. The server shuffles options per (session, question), so we map
    // through the same permutation to find the display position to send.
    private static int AnswerAsA2Learner(Guid sessionId, Guid questionId)
    {
        var question = QuestionBankSeed.Questions.Single(q => q.Id == questionId);
        var order = OptionShuffle.Order(sessionId, questionId, question.Options.Count);

        var knowsIt = question.Difficulty <= CefrLevel.A2;
        var targetOriginalIndex = knowsIt
            ? question.CorrectOptionIndex
            : (question.CorrectOptionIndex + 1) % question.Options.Count; // a wrong option
        return Array.IndexOf(order, targetOriginalIndex);
    }

    [Fact]
    public async Task Listening_audio_rejects_a_development_tone_and_never_leaks_the_script()
    {
        var client = _factory.CreateClient();

        var listening = QuestionBankSeed.Questions.First(q => q.Stage == TestStage.Listening);

        var audioResponse = await client.GetAsync($"/api/placement/audio/{listening.Id}");
        // The fixture deliberately uses LocalTextToSpeechService, whose WAV is a tone,
        // not spoken English. Production must never pass this off as a listening task.
        audioResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await audioResponse.Content.ReadAsStringAsync()).Should().Contain("placement_audio_unavailable");

        // The spoken script must never be exposed as readable JSON on the question DTO.
        var startResponse = await client.PostAsJsonAsync(
            "/api/placement/start", new { learnerId = Guid.NewGuid(), includeSpeaking = false });
        var start = await ReadJsonAsync(startResponse);
        start.GetProperty("firstItem").TryGetProperty("audioScript", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Audio_endpoint_404s_for_a_non_listening_item()
    {
        var client = _factory.CreateClient();
        var vocab = QuestionBankSeed.Questions.First(q => q.Stage == TestStage.Vocabulary);

        var response = await client.GetAsync($"/api/placement/audio/{vocab.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_with_empty_learner_id_returns_400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/placement/start",
            new { learnerId = Guid.Empty, includeSpeaking = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
