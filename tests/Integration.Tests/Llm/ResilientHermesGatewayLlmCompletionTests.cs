using System.Net;
using System.Text;
using FluentAssertions;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Llm;

[Collection("Hermes gateway environment")]
public class ResilientHermesGatewayLlmCompletionTests
{
    private sealed class ControlledHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _response;
        private int _active;
        public int PeakActive { get; private set; }

        public ControlledHandler(Func<CancellationToken, Task<HttpResponseMessage>> response) => _response = response;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _active);
            PeakActive = Math.Max(PeakActive, active);
            try
            {
                return await _response(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    [Fact]
    public async Task Bounds_concurrent_gateway_requests_and_drains_the_queue()
    {
        using var environment = EnvVarGuard.Clear(
            "HermesGateway__ApiKey", "HermesGateway__ApiKey2",
            "HERMES_GATEWAY_API_KEY", "HERMES_GATEWAY_API_KEY_2");
        var handler = new ControlledHandler(async cancellationToken =>
        {
            await Task.Delay(30, cancellationToken);
            return Success();
        });
        var resilient = Create(handler, maxConcurrency: 2, maxQueueLength: 20);

        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => resilient.CompleteAsync("system", "user", 64)));

        results.Should().OnlyContain(result => result == "ready");
        handler.PeakActive.Should().BeLessThanOrEqualTo(2);
        var snapshot = resilient.Snapshot();
        snapshot.Completed.Should().Be(12);
        snapshot.Active.Should().Be(0);
        snapshot.Queued.Should().Be(0);
    }

    [Fact]
    public async Task Opens_circuit_after_repeated_failures_and_rejects_without_throwing()
    {
        using var environment = EnvVarGuard.Clear(
            "HermesGateway__ApiKey", "HermesGateway__ApiKey2",
            "HERMES_GATEWAY_API_KEY", "HERMES_GATEWAY_API_KEY_2");
        var handler = new ControlledHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var resilient = Create(handler, circuitFailureThreshold: 2);

        (await resilient.CompleteAsync("system", "one", 64)).Should().BeNull();
        (await resilient.CompleteAsync("system", "two", 64)).Should().BeNull();
        (await resilient.CompleteAsync("system", "three", 64)).Should().BeNull();

        var snapshot = resilient.Snapshot();
        snapshot.CircuitOpen.Should().BeTrue();
        snapshot.Failed.Should().Be(2);
        snapshot.Rejected.Should().Be(1);
    }

    [Fact]
    public async Task Treats_transport_timeout_as_an_information_event_without_a_stack_trace()
    {
        using var environment = EnvVarGuard.Clear(
            "HermesGateway__ApiKey", "HermesGateway__ApiKey2",
            "HERMES_GATEWAY_API_KEY", "HERMES_GATEWAY_API_KEY_2");
        var handler = new ControlledHandler(_ => throw new TaskCanceledException("transport timeout"));
        var logger = new CapturingLogger();
        var resilient = Create(handler, logger: logger);

        (await resilient.CompleteAsync("system", "user", 64)).Should().BeNull();

        logger.Level.Should().Be(LogLevel.Information);
        logger.Exception.Should().BeNull();
        resilient.Snapshot().Failed.Should().Be(1);
    }

    private static ResilientHermesGatewayLlmCompletion Create(
        HttpMessageHandler handler,
        int maxConcurrency = 4,
        int maxQueueLength = 100,
        int circuitFailureThreshold = 5,
        ILogger<ResilientHermesGatewayLlmCompletion>? logger = null)
    {
        var options = new HermesGatewayOptions
        {
            ApiKey = "secret",
            MaxConcurrency = maxConcurrency,
            MaxQueueLength = maxQueueLength,
            QueueTimeoutSeconds = 5,
            CircuitFailureThreshold = circuitFailureThreshold,
            CircuitBreakSeconds = 30,
        };
        var inner = new HermesGatewayLlmCompletion(
            new StubFactory(handler), options, modelFallbacks: ["hermes-agent"], maxAttemptsPerModel: 1,
            baseBackoffMs: 1);
        return new ResilientHermesGatewayLlmCompletion(
            inner, options, logger ?? NullLogger<ResilientHermesGatewayLlmCompletion>.Instance);
    }

    private sealed class CapturingLogger : ILogger<ResilientHermesGatewayLlmCompletion>
    {
        public LogLevel? Level { get; private set; }
        public Exception? Exception { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Exception = exception;
        }
    }

    private static HttpResponseMessage Success() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            "{\"choices\":[{\"message\":{\"content\":\"ready\"},\"finish_reason\":\"stop\"}]}",
            Encoding.UTF8,
            "application/json"),
    };
}
