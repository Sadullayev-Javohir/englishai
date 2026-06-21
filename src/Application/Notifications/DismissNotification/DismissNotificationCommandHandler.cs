using Application.Notifications.Ports;
using MediatR;

namespace Application.Notifications.DismissNotification;

public sealed class DismissNotificationCommandHandler : IRequestHandler<DismissNotificationCommand>
{
    private readonly INotificationDismissalStore _dismissals;
    private readonly TimeProvider _clock;

    public DismissNotificationCommandHandler(INotificationDismissalStore dismissals, TimeProvider clock)
    {
        _dismissals = dismissals;
        _clock = clock;
    }

    public async Task Handle(DismissNotificationCommand request, CancellationToken cancellationToken)
    {
        await _dismissals.DismissAsync(
            request.LearnerId, request.NotificationId, _clock.GetUtcNow(), cancellationToken);
    }
}
