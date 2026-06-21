using Application.Admin.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Diagnostics;

public sealed class ServerTelemetrySampler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServerTelemetryStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<ServerTelemetrySampler> _logger;

    public ServerTelemetrySampler(
        IServiceScopeFactory scopeFactory,
        IServerTelemetryStore store,
        TimeProvider clock,
        ILogger<ServerTelemetrySampler> logger)
    {
        _scopeFactory = scopeFactory;
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SampleAsync(stoppingToken);
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_store.SampleIntervalSeconds), _clock);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await SampleAsync(stoppingToken);
    }

    private async Task SampleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var provider = scope.ServiceProvider.GetRequiredService<IServerDiagnosticsProvider>();
            _store.Append(await provider.CollectAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Server telemetry sample failed.");
        }
    }
}
