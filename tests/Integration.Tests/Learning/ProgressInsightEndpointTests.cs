using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Identity.Ports;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Integration.Tests.Learning;

public sealed class ProgressInsightEndpointTests
{
    [Fact]
    public async Task Authenticated_empty_learner_returns_stable_aggregate_contract_without_external_ai()
    {
        await using var factory = CreateFactory();
        using var client = CreateHttpsClient(factory);
        var learnerId = await SignInAsync(client);

        var response = await client.GetAsync(
            $"/api/learning/{learnerId}/progress-insight?today=2026-07-22");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("snapshot").GetProperty("hasLearningProfile").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("snapshot").GetProperty("vocabulary").GetProperty("total").GetInt32().Should().Be(0);
        json.RootElement.GetProperty("insight").GetProperty("overallCode").GetString().Should().Be("no_data");
        json.RootElement.GetProperty("insight").GetProperty("targetRoute").GetString().Should().Be("/assessment");
        json.RootElement.GetProperty("generatedAt").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task Authenticated_caller_cannot_read_another_learner()
    {
        await using var factory = CreateFactory();
        using var client = CreateHttpsClient(factory);
        await SignInAsync(client);

        var response = await client.GetAsync(
            $"/api/learning/{Guid.NewGuid()}/progress-insight?today=2026-07-22");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invalid_today_returns_bad_request()
    {
        await using var factory = CreateFactory();
        using var client = CreateHttpsClient(factory);
        var learnerId = await SignInAsync(client);

        var response = await client.GetAsync(
            $"/api/learning/{learnerId}/progress-insight?today=invalid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new TestWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:Jwt:SigningKey", "integration-test-signing-key-with-at-least-32-bytes");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IGoogleTokenValidator>();
                services.AddSingleton<IGoogleTokenValidator>(new StubGoogleTokenValidator());
                services.RemoveAll<IProgressInsightGenerator>();
                services.AddSingleton<IProgressInsightGenerator>(new StubProgressInsightGenerator());
            });
        });

    private static HttpClient CreateHttpsClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    private static async Task<Guid> SignInAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "valid" });
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("user").GetProperty("id").GetGuid();
    }

    private sealed class StubGoogleTokenValidator : IGoogleTokenValidator
    {
        public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken) =>
            Task.FromResult<GoogleUserInfo?>(new GoogleUserInfo(
                "progress-insight-subject", "progress@example.com", true, "Progress Learner", null));
    }

    private sealed class StubProgressInsightGenerator : IProgressInsightGenerator
    {
        public Task<GeneratedProgressInsight> GenerateAsync(
            ProgressSnapshotDto snapshot, CancellationToken cancellationToken) => Task.FromResult(
            new GeneratedProgressInsight("no_data", [], [], [], "no_study_data", "start_placement",
                "/assessment", ProgressInsightSource.Local, true));
    }
}
