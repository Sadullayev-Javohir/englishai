using System.Diagnostics;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Web.Observability;

namespace Integration.Tests;

public sealed class ObservabilityContractTests
{
    [Fact]
    public void Correlation_id_accepts_bounded_safe_values_and_replaces_invalid_values()
    {
        CorrelationIdMiddleware.ResolveCorrelationId("request-123").Should().Be("request-123");
        CorrelationIdMiddleware.ResolveCorrelationId("bad value").Should().NotBe("bad value");
        CorrelationIdMiddleware.ResolveCorrelationId(new string('a', 129)).Should().HaveLength(32);
    }

    [Fact]
    public async Task Correlation_handler_forwards_the_request_correlation_id()
    {
        var context = new DefaultHttpContext();
        context.Items[CorrelationConstants.ItemKey] = "request-123";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        var capture = new CapturingHandler();
        var handler = new CorrelationIdHandler(accessor) { InnerHandler = capture };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://example.test/");

        capture.CorrelationId.Should().Be("request-123");
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.1.2.3", true)]
    [InlineData("172.20.0.5", true)]
    [InlineData("192.168.1.10", true)]
    [InlineData("8.8.8.8", false)]
    public void Metrics_network_protection_only_accepts_private_addresses(string address, bool expected)
    {
        MetricsProtectionMiddleware.IsPrivate(IPAddress.Parse(address)).Should().Be(expected);
    }

    [Fact]
    public void Metrics_token_comparison_requires_an_exact_value()
    {
        MetricsProtectionMiddleware.FixedTimeEquals("secret", "secret").Should().BeTrue();
        MetricsProtectionMiddleware.FixedTimeEquals("secret", "Secret").Should().BeFalse();
        MetricsProtectionMiddleware.FixedTimeEquals("secret", "secret-extra").Should().BeFalse();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? CorrelationId { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CorrelationId = request.Headers.GetValues(CorrelationConstants.HeaderName).Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
