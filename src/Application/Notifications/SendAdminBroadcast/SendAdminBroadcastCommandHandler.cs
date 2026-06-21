using Application.Common;
using Application.Identity.Dtos;
using Application.Notifications.Dtos;
using Application.Notifications.Ports;
using Domain.Notifications;
using MediatR;

namespace Application.Notifications.SendAdminBroadcast;

/// <summary>
/// Persists a super-admin broadcast so it appears in every learner's feed. Authorizes the caller as a
/// super-admin (403 otherwise); ordinary admins cannot broadcast (they can only be managed). The
/// broadcast is stamped with the real send time, which is the arrival time learners see.
/// </summary>
public sealed class SendAdminBroadcastCommandHandler
    : IRequestHandler<SendAdminBroadcastCommand, AdminBroadcastDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IAdminBroadcastStore _broadcasts;
    private readonly INotificationRealtimeNotifier _realtime;
    private readonly IPushNotifier _push;
    private readonly TimeProvider _clock;

    public SendAdminBroadcastCommandHandler(
        IAdminAuthorization admin,
        IAdminBroadcastStore broadcasts,
        INotificationRealtimeNotifier realtime,
        IPushNotifier push,
        TimeProvider clock)
    {
        _admin = admin;
        _broadcasts = broadcasts;
        _realtime = realtime;
        _push = push;
        _clock = clock;
    }

    public async Task<AdminBroadcastDto> Handle(
        SendAdminBroadcastCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Only a super-admin can send notifications.");

        var broadcast = AdminBroadcast.Create(
            request.Title, request.Body, request.LinkUrl, _clock.GetUtcNow(), request.RequestingUserId);

        await _broadcasts.AddAsync(broadcast, cancellationToken);

        // Push the broadcast to connected learners so it lands in their feed immediately; it is already
        // persisted, so anyone offline still receives it on their next fetch.
        await _realtime.BroadcastSentAsync(
            NotificationDto.FromBroadcast(broadcast), cancellationToken);

        // Native push to every registered device so it also appears in the phone's status bar and sets
        // the app badge, even when the app is closed. Best-effort (the notifier swallows push failures);
        // the admin-composed title/body are shown as-is (not AI-generated, so rule 11 is satisfied).
        await _push.NotifyAllAsync(
            new PushMessage(broadcast.Title, broadcast.Body, broadcast.LinkUrl), cancellationToken);

        return AdminBroadcastDto.FromDomain(broadcast);
    }
}
