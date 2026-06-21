using Application.Admin.Ports;
using FluentAssertions;
using Infrastructure.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;
using Web.Logging;
using Xunit;

namespace Integration.Tests.Diagnostics;

public sealed class RecentLogSinkTests
{
    [Fact]
    public void Emit_captures_request_and_trace_metadata()
    {
        IRecentLogStore store = new InMemoryRecentLogStore();
        var sink = new RecentLogSink(store);
        var properties = new[]
        {
            new LogEventProperty("SourceContext", new ScalarValue("Microsoft.EntityFrameworkCore.Query")),
            new LogEventProperty("RequestPath", new ScalarValue("/api/writing/submit")),
            new LogEventProperty("CorrelationId", new ScalarValue("corr-123")),
            new LogEventProperty("TraceId", new ScalarValue("trace-456")),
        };
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            null,
            new MessageTemplate("Database query failed", Array.Empty<MessageTemplateToken>()),
            properties);

        sink.Emit(logEvent);

        var entry = store.Snapshot().Should().ContainSingle().Subject;
        entry.SourceContext.Should().Be("Microsoft.EntityFrameworkCore.Query");
        entry.RequestPath.Should().Be("/api/writing/submit");
        entry.CorrelationId.Should().Be("corr-123");
        entry.TraceId.Should().Be("trace-456");
    }

    [Fact]
    public void Emit_ignores_information_events()
    {
        IRecentLogStore store = new InMemoryRecentLogStore();
        var sink = new RecentLogSink(store);
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Healthy", Array.Empty<MessageTemplateToken>()),
            Array.Empty<LogEventProperty>());

        sink.Emit(logEvent);

        store.Snapshot().Should().BeEmpty();
    }
}
