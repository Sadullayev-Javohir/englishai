using Hangfire;
using Infrastructure.Jobs;
using System.Diagnostics.Metrics;

namespace Worker;

public sealed class HangfireMetrics
{
    private readonly ILogger<HangfireMetrics> _logger;

    public HangfireMetrics(ILogger<HangfireMetrics> logger)
    {
        _logger = logger;

        foreach (var queue in HangfireQueues.All)
        {
            var capturedQueue = queue;
            WorkerTelemetry.Meter.CreateObservableGauge(
                "englishai.hangfire.queue.enqueued",
                () => new Measurement<long>(Read(api => api.EnqueuedCount(capturedQueue)), new KeyValuePair<string, object?>("queue", capturedQueue)));
        }

        WorkerTelemetry.Meter.CreateObservableGauge("englishai.hangfire.processing", () => Read(api => api.ProcessingCount()));
        WorkerTelemetry.Meter.CreateObservableGauge("englishai.hangfire.scheduled", () => Read(api => api.ScheduledCount()));
        WorkerTelemetry.Meter.CreateObservableGauge("englishai.hangfire.failed", () => Read(api => api.FailedCount()));
        WorkerTelemetry.Meter.CreateObservableGauge("englishai.hangfire.servers", () => Read(api => api.Servers().Count));
    }

    private long Read(Func<Hangfire.Storage.IMonitoringApi, long> select)
    {
        try
        {
            return select(JobStorage.Current.GetMonitoringApi());
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Hangfire metrics snapshot unavailable.");
            return 0;
        }
    }
}
