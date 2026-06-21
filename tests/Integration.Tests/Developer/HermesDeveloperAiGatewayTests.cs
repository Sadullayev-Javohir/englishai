using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Developer;
using Xunit;

namespace Integration.Tests.Developer;

public sealed class HermesDeveloperAiGatewayTests
{
    private sealed class ControlledHandler(Func<CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => response(cancellationToken);
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    [Fact]
    public async Task Maps_an_upstream_transport_timeout_to_gateway_timeout()
    {
        var gateway = Create(_ => throw new TaskCanceledException("transport timeout"));

        await using var response = await gateway.SendChatCompletionAsync(Request(), CancellationToken.None);

        response.Response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
        (await ErrorCode(response.Response)).Should().Be("upstream_timeout");
    }

    [Fact]
    public async Task Maps_an_upstream_connection_failure_to_bad_gateway()
    {
        var gateway = Create(_ => throw new HttpRequestException("connection refused"));

        await using var response = await gateway.SendChatCompletionAsync(Request(), CancellationToken.None);

        response.Response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        (await ErrorCode(response.Response)).Should().Be("upstream_unavailable");
    }

    [Fact]
    public async Task Preserves_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var gateway = Create(token => Task.FromCanceled<HttpResponseMessage>(token));

        var act = () => gateway.SendChatCompletionAsync(Request(), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Returns_successful_upstream_responses_unchanged()
    {
        var gateway = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"choices\":[]}", Encoding.UTF8, "application/json"),
        }));

        await using var response = await gateway.SendChatCompletionAsync(Request(), CancellationToken.None);

        response.Response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Response.Content.ReadAsStringAsync()).Should().Be("{\"choices\":[]}");
    }

    private static HermesDeveloperAiGateway Create(
        Func<CancellationToken, Task<HttpResponseMessage>> response) =>
        new(new StubFactory(new ControlledHandler(response)), new DeveloperAiOptions
        {
            ApiKeys = ["secret"],
            BaseUrl = "http://provider.test/v1",
            Model = "hermes-agent",
        });

    private static JsonElement Request() => JsonSerializer.SerializeToElement(new
    {
        messages = new[] { new { role = "user", content = "Hello" } },
    });

    private static async Task<string> ErrorCode(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("error").GetProperty("code").GetString()!;
    }
}
