using Application.Notifications.Ports;
using MediatR;

namespace Application.Notifications.RegisterDevice;

/// <summary>Upserts the caller's device token (registration is idempotent per token).</summary>
public sealed class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Unit>
{
    private readonly IDeviceTokenStore _store;
    private readonly TimeProvider _clock;

    public RegisterDeviceCommandHandler(IDeviceTokenStore store, TimeProvider clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<Unit> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        await _store.UpsertAsync(
            request.LearnerId, request.Token, request.Platform, _clock.GetUtcNow(), cancellationToken);
        return Unit.Value;
    }
}
