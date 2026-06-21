using Application.Admin.Dtos;
using Application.Admin.Ports;
using Serilog.Core;
using Serilog.Events;

namespace Web.Logging;

/// <summary>
/// A Serilog sink that mirrors warning-and-above log lines into the in-process
/// <see cref="IRecentLogStore"/>, so the super-admin server page can show "what's going wrong right
/// now" without a log-aggregation backend. Only Warning/Error/Fatal are kept (the buffer is a
/// diagnostics tail, not a full log), and it never throws or blocks - a diagnostics sink must never
/// take down the logging pipeline.
/// </summary>
public sealed class RecentLogSink : ILogEventSink
{
    private readonly IRecentLogStore _store;

    public RecentLogSink(IRecentLogStore store)
    {
        _store = store;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent is null || logEvent.Level < LogEventLevel.Warning)
            return;

        try
        {
            _store.Add(new LogEntryDto(
                TimestampUtc: logEvent.Timestamp.ToUniversalTime(),
                Level: logEvent.Level.ToString(),
                Message: logEvent.RenderMessage(),
                Exception: logEvent.Exception?.ToString(),
                Id: Guid.NewGuid(),
                SourceContext: Property(logEvent, "SourceContext"),
                RequestPath: Property(logEvent, "RequestPath"),
                CorrelationId: Property(logEvent, "CorrelationId"),
                TraceId: Property(logEvent, "TraceId")));
        }
        catch
        {
            // A diagnostics buffer must never disrupt logging; swallow any failure.
        }
    }

    private static string? Property(LogEvent logEvent, string name)
    {
        if (!logEvent.Properties.TryGetValue(name, out var value))
            return null;

        var rendered = value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : value.ToString();
        return string.IsNullOrWhiteSpace(rendered) ? null : rendered;
    }
}
