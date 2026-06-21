using Application.Retention.RunRetentionSweep;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Retention;

/// <summary>
/// Hangfire recurring-job entry point for the daily win-back sweep (PROJECT-SPEC I.2). A
/// thin adapter: all logic lives in <see cref="RunRetentionSweepCommand"/>'s handler, which
/// is unit-tested independently. Hangfire resolves this from DI and invokes <see cref="RunAsync"/>.
/// </summary>
public sealed class RetentionSweepJob
{
    private readonly ISender _sender;
    private readonly ILogger<RetentionSweepJob> _logger;

    public RetentionSweepJob(ISender sender, ILogger<RetentionSweepJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var result = await _sender.Send(new RunRetentionSweepCommand());
        _logger.LogInformation(
            "Retention sweep: scanned {Scanned} learner(s), sent {Sent} win-back message(s).",
            result.ScannedLearners, result.WinBackMessagesSent);
    }
}
