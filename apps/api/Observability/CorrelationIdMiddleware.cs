using System.Diagnostics;
using Serilog.Context;

namespace Web.Observability;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[CorrelationConstants.HeaderName]);
        context.Items[CorrelationConstants.ItemKey] = correlationId;
        context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;

        var activity = Activity.Current;
        activity?.SetTag("correlation.id", correlationId);
        activity?.AddBaggage("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", activity?.TraceId.ToString() ?? string.Empty))
        using (LogContext.PushProperty("SpanId", activity?.SpanId.ToString() ?? string.Empty))
            await next(context);
    }

    public static string ResolveCorrelationId(string? candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= MaxLength && candidate.All(IsAllowed))
            return candidate;

        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }

    private static bool IsAllowed(char value) =>
        char.IsLetterOrDigit(value) || value is '-' or '_' or '.' or ':';
}
