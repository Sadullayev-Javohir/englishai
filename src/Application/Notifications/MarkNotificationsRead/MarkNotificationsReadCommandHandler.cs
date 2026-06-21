using Application.Notifications.Ports;
using MediatR;

namespace Application.Notifications.MarkNotificationsRead;

public sealed class MarkNotificationsReadCommandHandler : IRequestHandler<MarkNotificationsReadCommand>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly INotificationReadStateStore _readState;
    private readonly TimeProvider _clock;

    public MarkNotificationsReadCommandHandler(
        INotificationDispatcher dispatcher,
        INotificationReadStateStore readState,
        TimeProvider clock)
    {
        _dispatcher = dispatcher;
        _readState = readState;
        _clock = clock;
    }

    public async Task Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        // Advancing the durable watermark is what dismisses the derived SRS + daily reminders
        // (their read-state is decided by CreatedAt vs. this timestamp). Also flag the stored
        // lifecycle notifications so a future durable dispatcher stays consistent.
        await _readState.SetLastReadAtAsync(request.LearnerId, _clock.GetUtcNow(), cancellationToken);
        await _dispatcher.MarkAllReadAsync(request.LearnerId, cancellationToken);
    }
}
