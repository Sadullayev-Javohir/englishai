using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests;

public sealed class MandatoryVocabularyReviewGateTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MandatoryVocabularyReviewGateTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Due_review_is_reported_but_does_not_block_learning_api()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/dev-login", new { });
        login.EnsureSuccessStatusCode();
        var loginJson = await ReadJsonAsync(login);
        var learnerId = loginJson.GetProperty("user").GetProperty("id").GetGuid();

        var repository = _factory.Services.GetRequiredService<IVocabularyRepository>();
        var item = VocabularyItem.Learn(
            learnerId,
            "journey",
            "sayohat",
            DateTimeOffset.UtcNow.AddDays(-3),
            sourceTopicId: Guid.NewGuid());
        await repository.SaveAsync(item, CancellationToken.None);

        var statusResponse = await client.GetAsync($"/api/vocabulary/{learnerId}/review-status");
        statusResponse.EnsureSuccessStatusCode();
        var status = await ReadJsonAsync(statusResponse);
        status.GetProperty("isRequired").GetBoolean().Should().BeTrue();
        status.GetProperty("dueItemCount").GetInt32().Should().Be(1);
        status.GetProperty("dueTopicCount").GetInt32().Should().Be(1);

        var learning = await client.GetAsync($"/api/learning/{learnerId}/overview");
        learning.StatusCode.Should().NotBe((HttpStatusCode)423);

        var due = await client.GetAsync($"/api/vocabulary/{learnerId}/due");
        due.EnsureSuccessStatusCode();

        var review = await client.PostAsJsonAsync("/api/vocabulary/review", new
        {
            vocabularyItemId = item.Id,
            submittedAnswer = "journey"
        });
        review.EnsureSuccessStatusCode();

        var openStatus = await client.GetAsync($"/api/vocabulary/{learnerId}/review-status");
        openStatus.EnsureSuccessStatusCode();
        (await ReadJsonAsync(openStatus)).GetProperty("isRequired").GetBoolean().Should().BeFalse();

        var stillOpen = await client.GetAsync($"/api/learning/{learnerId}/overview");
        stillOpen.StatusCode.Should().NotBe((HttpStatusCode)423);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
