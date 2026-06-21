using Application.Subscription.ProcessExpiry;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Subscription;

/// <summary>
/// Hangfire recurring-job entry point for daily subscription maintenance (PROJECT-SPEC H.3).
/// A thin adapter: all logic lives in <see cref="ProcessSubscriptionExpiryCommand"/>'s
/// handler, which is unit-tested independently.
/// </summary>
public sealed class SubscriptionExpiryJob
{
    private readonly ISender _sender;
    private readonly ILogger<SubscriptionExpiryJob> _logger;

    public SubscriptionExpiryJob(ISender sender, ILogger<SubscriptionExpiryJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var result = await _sender.Send(new ProcessSubscriptionExpiryCommand());
        _logger.LogInformation(
            "Subscription maintenance: {Reminded} renewal reminder(s), {Expired} expired.",
            result.RemindedLearners, result.ExpiredSubscriptions);
    }
}
