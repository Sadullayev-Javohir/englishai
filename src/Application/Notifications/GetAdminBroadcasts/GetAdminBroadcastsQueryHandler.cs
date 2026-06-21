using Application.Common;
using Application.Identity.Dtos;
using Application.Notifications.Dtos;
using Application.Notifications.Ports;
using MediatR;

namespace Application.Notifications.GetAdminBroadcasts;

/// <summary>
/// Returns the recently sent broadcasts for the admin console. Authorizes the caller as a super-admin
/// (403 otherwise) - only the operator who can send them may review the history.
/// </summary>
public sealed class GetAdminBroadcastsQueryHandler
    : IRequestHandler<GetAdminBroadcastsQuery, CursorPage<AdminBroadcastDto>>
{
    private readonly IAdminAuthorization _admin;
    private readonly IAdminBroadcastStore _broadcasts;

    public GetAdminBroadcastsQueryHandler(IAdminAuthorization admin, IAdminBroadcastStore broadcasts)
    {
        _admin = admin;
        _broadcasts = broadcasts;
    }

    public async Task<CursorPage<AdminBroadcastDto>> Handle(
        GetAdminBroadcastsQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Only a super-admin can view broadcasts.");

        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var cursor = StableCursor.Decode(request.Cursor);
        var recent = await _broadcasts.GetRecentAsync(
            cursor?.Timestamp, cursor?.Id, pageSize + 1, cancellationToken);
        var items = recent.Take(pageSize).Select(AdminBroadcastDto.FromDomain).ToList();
        return new CursorPage<AdminBroadcastDto>(items, recent.Count > pageSize
            ? new StableCursor(recent[pageSize - 1].CreatedAt, recent[pageSize - 1].Id).Encode()
            : null);
    }
}
