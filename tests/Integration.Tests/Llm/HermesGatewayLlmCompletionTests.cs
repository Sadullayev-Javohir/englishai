using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Llm;
using Xunit;

namespace Integration.Tests.Llm;

[Collection("Hermes gateway environment")]
public class HermesGatewayLlmCompletionTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? Authorization { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"content":"ready"},"finish_reason":"stop"}]}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    [Fact]
    public async Task Sends_single_turn_to_local_gateway_with_auth_and_caller_token_cap()
    {
        var handler = new CapturingHandler();
        var completion = Create(handler);

        var result = await completion.CompleteAsync("system", "user", 64, CancellationToken.None);

        result.Should().Be("ready");
        handler.RequestUri.Should().Be("http://127.0.0.1:8642/v1/chat/completions");
        handler.Authorization.Should().Be("Bearer local-secret");
        using var request = JsonDocument.Parse(handler.RequestBody!);
        request.RootElement.GetProperty("model").GetString().Should().Be("hermes-agent");
        // The caller's 64-token cap is raised to the shared reasoning-model floor: the gateway's model
        // thinks before it answers, and a cap sized for the answer alone is spent entirely on the
        // hidden trace, returning finish_reason "length" with no content.
        request.RootElement.GetProperty("max_tokens").GetInt32().Should().Be(1024);
        request.RootElement.GetProperty("reasoning").GetProperty("exclude").GetBoolean().Should().BeTrue();
        request.RootElement.GetProperty("messages").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Sends_complete_multi_turn_history_through_same_client()
    {
        var handler = new CapturingHandler();
        var completion = Create(handler);
        var turns = new[]
        {
            (IsUser: true, Text: "Hello"),
            (IsUser: false, Text: "Hi"),
            (IsUser: true, Text: "Continue"),
        };

        var result = await completion.CompleteConversationAsync("system", turns, 200);

        result.Should().Be("ready");
        using var request = JsonDocument.Parse(handler.RequestBody!);
        request.RootElement.GetProperty("max_tokens").GetInt32().Should().Be(1024);
        request.RootElement.GetProperty("reasoning").GetProperty("exclude").GetBoolean().Should().BeTrue();
        var messages = request.RootElement.GetProperty("messages");
        messages.GetArrayLength().Should().Be(4);
        messages[1].GetProperty("role").GetString().Should().Be("user");
        messages[2].GetProperty("role").GetString().Should().Be("assistant");
        messages[3].GetProperty("content").GetString().Should().Be("Continue");
    }

    [Fact]
    public async Task Always_uses_the_router_key_even_when_a_secondary_agent_key_exists()
    {
        var handler = new CapturingHandler();
        var completion = new HermesGatewayLlmCompletion(
            new StubHttpClientFactory(handler),
            new HermesGatewayOptions
            {
                ApiKey = "router-key",
                Endpoint = "http://127.0.0.1:8642/v1",
                Model = "hermes-agent",
            },
            modelFallbacks: new[] { "hermes-agent" },
            maxAttemptsPerModel: 1,
            baseBackoffMs: 1);

        (await completion.CompleteAsync("system", "user", 64)).Should().Be("ready");
        (await completion.CompleteAsync("system", "user", 64)).Should().Be("ready");

        handler.Authorization.Should().Be("Bearer router-key");
    }

    [Fact]
    public async Task Treats_a_gateway_error_completion_as_unavailable()
    {
        var handler = new StaticResponseHandler(
            "{\"choices\":[{\"message\":{\"content\":\"upstream key invalid\"},\"finish_reason\":\"error\"}]}");
        var completion = Create(handler);

        var result = await completion.CompleteAsync("system", "user", 64);

        result.Should().BeNull();
    }

    private static HermesGatewayLlmCompletion Create(HttpMessageHandler handler) =>
        new(
            new StubHttpClientFactory(handler),
            new HermesGatewayOptions
            {
                ApiKey = "local-secret",
                Endpoint = "http://127.0.0.1:8642/v1",
                Model = "hermes-agent",
            },
            modelFallbacks: new[] { "hermes-agent" },
            maxAttemptsPerModel: 1,
            baseBackoffMs: 1);

    private sealed class StaticResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
