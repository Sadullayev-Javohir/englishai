using Application.Notifications.Dtos;
using Application.Common;
using MediatR;

namespace Application.Notifications.GetAdminBroadcasts;

/// <summary>Lists the most recent super-admin broadcasts for the operator console.</summary>
public sealed record GetAdminBroadcastsQuery(Guid RequestingUserId, string? Cursor = null, int PageSize = 20)
    : IRequest<CursorPage<AdminBroadcastDto>>;
