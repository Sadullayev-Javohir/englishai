using Application.Identity.ProcessTrialExpiry;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

/// <summary>
/// Hangfire recurring-job entry point for the free Pro trial reminders. A thin adapter: the logic
/// lives in <see cref="ProcessTrialExpiryCommand"/>'s handler, which is unit-tested independently.
/// </summary>
public sealed class TrialExpiryJob
{
    private readonly ISender _sender;
    private readonly ILogger<TrialExpiryJob> _logger;

    public TrialExpiryJob(ISender sender, ILogger<TrialExpiryJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var result = await _sender.Send(new ProcessTrialExpiryCommand());
        _logger.LogInformation(
            "Trial maintenance: {Reminded} reminder(s), {Expired} trial(s) ended.",
            result.RemindedLearners, result.ExpiredTrials);
    }
}
