using Application.Common;
using Application.Identity.Dtos;
using Application.Notifications.Ports;
using MediatR;

namespace Application.Notifications.DeleteAdminBroadcast;

/// <summary>
/// Deletes a sent broadcast from the operator history. Authorizes the caller as a super-admin (403
/// otherwise) - only the operator who can send broadcasts may remove them.
/// </summary>
public sealed class DeleteAdminBroadcastCommandHandler : IRequestHandler<DeleteAdminBroadcastCommand, bool>
{
    private readonly IAdminAuthorization _admin;
    private readonly IAdminBroadcastStore _broadcasts;

    public DeleteAdminBroadcastCommandHandler(IAdminAuthorization admin, IAdminBroadcastStore broadcasts)
    {
        _admin = admin;
        _broadcasts = broadcasts;
    }

    public async Task<bool> Handle(DeleteAdminBroadcastCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Only a super-admin can delete broadcasts.");

        return await _broadcasts.DeleteAsync(request.BroadcastId, cancellationToken);
    }
}
