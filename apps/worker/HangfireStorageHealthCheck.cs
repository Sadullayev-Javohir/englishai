using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Worker;

public sealed class HangfireStorageHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = JobStorage.Current.GetMonitoringApi().Servers();
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is unavailable.", exception));
        }
    }
}
