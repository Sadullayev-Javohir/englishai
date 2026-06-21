using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// End-to-end test of the Speaking module over HTTP: start a conversation, submit an
/// utterance through the offline pipeline, and fetch a word's pronunciation detail.
/// </summary>
public class SpeakingFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SpeakingFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Offline_speech_provider_never_fabricates_a_learner_turn()
    {
        var client = _factory.CreateClient();

        var startResponse = await client.PostAsJsonAsync(
            "/api/speaking/start",
            new { learnerId = Guid.NewGuid(), level = 3 }); // 3 = B1
        startResponse.EnsureSuccessStatusCode();

        var start = await ReadJsonAsync(startResponse);
        var sessionId = start.GetProperty("sessionId").GetGuid();
        start.GetProperty("tutorText").GetString().Should().NotBeNullOrWhiteSpace();
        start.GetProperty("visemes").GetArrayLength().Should().BeGreaterThan(0);

        var utteranceResponse = await client.PostAsJsonAsync(
            "/api/speaking/utterance",
            new { sessionId, audioContent = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }) });
        utteranceResponse.EnsureSuccessStatusCode();

        var utterance = await ReadJsonAsync(utteranceResponse);
        utterance.GetProperty("recognized").GetBoolean().Should().BeFalse();
        utterance.GetProperty("recognizedText").GetString().Should().BeEmpty();
        utterance.GetProperty("tutorText").GetString().Should().BeEmpty();
        utterance.GetProperty("pronunciation").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Word_detail_returns_ipa_for_known_word()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/speaking/word/three");
        response.EnsureSuccessStatusCode();

        var detail = await ReadJsonAsync(response);
        detail.GetProperty("ipa").GetString().Should().Be("θriː");
        detail.GetProperty("phonemes").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Word_detail_returns_404_for_unknown_word()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/speaking/word/zzzzz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/speaking/accent-tutors/american/start")]
    [InlineData("/api/speaking/accent-tutors/british/start")]
    [InlineData("/api/speaking/accent-tutors/australian/start")]
    [InlineData("/api/speaking/accent-tutors/irish/start")]
    [InlineData("/api/speaking/accent-tutors/american/nudge")]
    [InlineData("/api/speaking/accent-tutors/american/evaluate")]
    [InlineData("/api/speaking/accent-tutors/american/turn/stream")]
    public async Task Accent_tutor_start_route_is_mapped_and_requires_authentication(string route)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.PostAsJsonAsync(route, new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
