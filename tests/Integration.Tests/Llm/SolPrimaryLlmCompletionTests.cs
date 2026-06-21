using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Llm;

public sealed class SolPrimaryLlmCompletionTests
{
    [Fact]
    public void Configured_primary_fallback_resolves_from_the_application_container()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HermesGateway:Endpoint"] = "https://hermes.example.test",
                ["HermesGateway:ApiKey"] = "hermes-key",
                ["SolPrimary:Endpoint"] = "https://sol.example.test",
                ["SolPrimary:ApiKey"] = "sol-key",
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = false,
                ValidateScopes = false,
            });

        provider.GetRequiredService<PrimaryFallbackLlmCompletion>().Should().NotBeNull();
        provider.GetRequiredService<ResilientHermesGatewayLlmCompletion>().Should().NotBeNull();
    }

    [Fact]
    public void Every_registered_llm_feature_uses_the_single_resilient_completion_graph()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Infrastructure", "DependencyInjection.cs"));

        source.Should().Contain("ILlmCompletion ContentCompletion(IServiceProvider sp) => ContentLlm.Completion(");
        source.Should().Contain("sp.GetRequiredService<ResilientHermesGatewayLlmCompletion>()");
        source.Should().NotContain("new SolPrimaryLlmCompletion(");
        source.Should().NotMatchRegex(@"new\s+(?:Hermes|Llm)\w+\(\s*sp\.GetRequiredService<HermesGatewayLlmCompletion>");

        var featureRegistrations = source.Split('\n')
            .Where(line => line.Contains("ContentCompletion(sp)", StringComparison.Ordinal))
            .ToArray();
        featureRegistrations.Should().HaveCountGreaterThanOrEqualTo(18);
    }

    [Fact]
    public void Non_content_ai_integrations_are_explicitly_isolated_from_the_learner_text_graph()
    {
        var infrastructure = Path.Combine(FindRepositoryRoot(), "src", "Infrastructure");
        var developer = File.ReadAllText(Path.Combine(infrastructure, "Developer", "HermesDeveloperAiGateway.cs"));
        var backfill = File.ReadAllText(Path.Combine(infrastructure, "Curriculum", "EdubaseBackfillClient.cs"));
        var moderation = File.ReadAllText(Path.Combine(infrastructure, "Images", "SafetyCheckedImageService.cs"));
        var speech = File.ReadAllText(Path.Combine(infrastructure, "Speaking", "AzureSpeechToTextService.cs"));

        developer.Should().Contain("IDeveloperAiGateway");
        developer.Should().NotContain("ILlmCompletion");
        backfill.Should().Contain("EdubaseBackfillClient");
        backfill.Should().NotContain("ILlmCompletion");
        moderation.Should().Contain("Image moderation failed closed");
        speech.Should().Contain("IVariableCostMeter");
    }

    [Fact]
    public async Task Sends_azure_api_key_and_max_completion_tokens()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"ready\"},\"finish_reason\":\"stop\"}]}");
        var completion = Create(handler);

        var result = await completion.CompleteAsync("system", "user", 321);

        result.Should().Be("ready");
        handler.RequestUri.Should().Be("https://example.azure-api.net/resource/openai/v1/chat/completions");
        handler.ApiKey.Should().Be("secret");
        using var request = JsonDocument.Parse(handler.RequestBody!);
        request.RootElement.GetProperty("model").GetString().Should().Be("gpt-5.6-sol");
        request.RootElement.GetProperty("max_completion_tokens").GetInt32().Should().Be(321);
        request.RootElement.TryGetProperty("max_tokens", out _).Should().BeFalse();
        // gpt-5.x SOL is a reasoning model; without this it burns the whole output budget on hidden
        // reasoning and returns empty content, which the fallback layer reads as an outage.
        request.RootElement.GetProperty("reasoning_effort").GetString().Should().Be("none");
    }

    [Fact]
    public async Task Omits_reasoning_effort_when_blank()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"ready\"},\"finish_reason\":\"stop\"}]}");
        var completion = Create(handler, new SolPrimaryOptions
        {
            Endpoint = "https://example.azure-api.net/resource",
            ApiKey = "secret",
            Model = "gpt-5.6-sol",
            ReasoningEffort = "",
        });

        await completion.CompleteAsync("system", "user", 321);

        using var request = JsonDocument.Parse(handler.RequestBody!);
        request.RootElement.TryGetProperty("reasoning_effort", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Primary_failure_falls_back_to_hermes()
    {
        var primary = Create(new CapturingHandler(HttpStatusCode.ServiceUnavailable, "{}"));
        var fallback = new StubConversationCompletion("fallback");
        var composite = new PrimaryFallbackLlmCompletion(
            primary,
            fallback,
            NullLogger<PrimaryFallbackLlmCompletion>.Instance);

        var result = await composite.CompleteAsync("system", "user", 64);

        result.Should().Be("fallback");
        fallback.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Token_reservation_prevents_provider_overdraw_before_another_http_call()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"ready\"}}]}");
        var completion = Create(handler, new SolPrimaryOptions
        {
            Endpoint = "https://example.azure-api.net/resource",
            ApiKey = "secret",
            Model = "gpt-5.6-sol",
            RequestTimeoutSeconds = 5,
            TokenLimitPerWindow = 100,
            TokenReservationPerRequest = 60,
            TokenSafetyReserve = 10,
            TokenWindowSeconds = 60,
        });

        (await completion.CompleteAsync("system", "first", 32)).Should().Be("ready");
        (await completion.CompleteAsync("system", "second", 32)).Should().BeNull();

        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Provider_429_closes_the_local_token_window_for_following_calls()
    {
        var handler = new CapturingHandler(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"type\":\"too_many_requests\"}}");
        var completion = Create(handler);

        (await completion.CompleteAsync("system", "first", 32)).Should().BeNull();
        (await completion.CompleteAsync("system", "second", 32)).Should().BeNull();

        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Reserved_token_window_routes_following_request_to_hermes_fallback()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"primary\"}}]}");
        var primary = Create(handler, new SolPrimaryOptions
        {
            Endpoint = "https://example.azure-api.net/resource",
            ApiKey = "secret",
            Model = "gpt-5.6-sol",
            RequestTimeoutSeconds = 5,
            TokenLimitPerWindow = 100,
            TokenReservationPerRequest = 60,
            TokenSafetyReserve = 10,
        });
        var fallback = new StubConversationCompletion("hermes");
        var composite = new PrimaryFallbackLlmCompletion(
            primary, fallback, NullLogger<PrimaryFallbackLlmCompletion>.Instance);

        (await composite.CompleteAsync("system", "first", 32)).Should().Be("primary");
        (await composite.CompleteAsync("system", "second", 32)).Should().Be("hermes");

        handler.Calls.Should().Be(1);
        fallback.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Busy_primary_routes_parallel_request_to_hermes_without_waiting()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ControlledHandler(async cancellationToken =>
        {
            await release.Task.WaitAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"primary\"}}]}",
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        var primary = Create(handler, new SolPrimaryOptions
        {
            Endpoint = "https://example.azure-api.net/resource",
            ApiKey = "secret",
            Model = "gpt-5.6-sol",
            // The full integration suite can briefly starve continuations on the single self-hosted
            // runner. Keep the provider call alive long enough for this test to release it; the assertion
            // below still gives the busy second call a strict deadline.
            RequestTimeoutSeconds = 120,
            MaxConcurrency = 1,
        });
        var fallback = new StubConversationCompletion("hermes");
        var composite = new PrimaryFallbackLlmCompletion(
            primary, fallback, NullLogger<PrimaryFallbackLlmCompletion>.Instance);

        var first = composite.CompleteAsync("system", "first", 32);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
        var second = await composite.CompleteAsync("system", "second", 32)
            .WaitAsync(TimeSpan.FromSeconds(30));
        release.SetResult();

        second.Should().Be("hermes");
        (await first).Should().Be("primary");
        fallback.Calls.Should().Be(1);
    }

    private static SolPrimaryLlmCompletion Create(HttpMessageHandler handler, SolPrimaryOptions? options = null) => new(
        new StubFactory(handler),
        options ?? new SolPrimaryOptions
        {
            Endpoint = "https://example.azure-api.net/resource",
            ApiKey = "secret",
            Model = "gpt-5.6-sol",
            RequestTimeoutSeconds = 5,
        },
        NullLogger<SolPrimaryLlmCompletion>.Instance);

    private sealed class CapturingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? RequestUri { get; private set; }
        public string? ApiKey { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            RequestUri = request.RequestUri?.ToString();
            ApiKey = request.Headers.TryGetValues("api-key", out var values) ? values.Single() : null;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class ControlledHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            return response(cancellationToken);
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class StubConversationCompletion(string response) : IConversationLlmCompletion
    {
        public int Calls { get; private set; }
        public string Model => "hermes-agent";

        public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, int maxOutputTokens,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<string?>(response);
        }

        public Task<string?> CompleteConversationAsync(string systemPrompt,
            IReadOnlyList<(bool IsUser, string Text)> turns, int maxOutputTokens,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<string?>(response);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EnglishAI.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
