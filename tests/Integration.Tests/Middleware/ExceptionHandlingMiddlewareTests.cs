using Application.Speaking;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Infrastructure.Speaking;
using System.Text.Json;
using Web.Middleware;
using Xunit;

namespace Integration.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Client_cancellation_is_not_logged_as_an_unhandled_error()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var context = new DefaultHttpContext
        {
            RequestAborted = cancellation.Token,
        };
        context.Response.Body = new MemoryStream();
        var logger = new CapturingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(cancellation.Token),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status499ClientClosedRequest);
        context.Response.Body.Length.Should().Be(0);
        logger.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task Speaking_tutor_unavailable_is_returned_as_retryable_service_unavailable()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = new CapturingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new SpeakingTutorUnavailableException("timeout"),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        payload.RootElement.GetProperty("code").GetString().Should().Be("timeout");
        payload.RootElement.GetProperty("retryable").GetBoolean().Should().BeTrue();
        logger.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task Accent_tutor_configuration_error_is_sanitized_and_non_retryable()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = new CapturingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new SpeakingTutorUnavailableException(
                "accent_tutor_not_configured",
                new InvalidOperationException("secret provider detail"),
                retryable: false),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        payload.RootElement.GetProperty("code").GetString().Should().Be("accent_tutor_not_configured");
        payload.RootElement.GetProperty("message").GetString().Should().Be("Speaking AI is temporarily unavailable.");
        payload.RootElement.GetProperty("message").GetString().Should().NotContain("secret provider detail");
        payload.RootElement.GetProperty("retryable").GetBoolean().Should().BeFalse();
        logger.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task Unhandled_error_returns_machine_code_and_correlation_id()
    {
        var context = new DefaultHttpContext();
        context.Items[Web.Observability.CorrelationConstants.ItemKey] = "book-submit-123";
        context.Response.Body = new MemoryStream();
        var logger = new CapturingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("database failed"),
            logger);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        payload.RootElement.GetProperty("code").GetString().Should().Be("unexpected_error");
        payload.RootElement.GetProperty("correlationId").GetString().Should().Be("book-submit-123");
        payload.RootElement.GetProperty("message").GetString().Should().NotContain("database failed");
        logger.ErrorCount.Should().Be(1);
    }

    private sealed class CapturingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public int ErrorCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
                ErrorCount++;
        }
    }
}
