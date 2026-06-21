using Application.Admin.Dtos;
using MediatR;

namespace Application.Admin.GetAdminUsers;

/// <summary>
/// Lists every registered account for the admin panel. <see cref="RequestingUserId"/> is the viewer
/// (from the session); the handler authorizes it as an admin and rejects everyone else with a 403.
/// Named <c>RequestingUserId</c> (not <c>LearnerId</c>) so the ownership pipeline behavior ignores it.
/// </summary>
public sealed record GetAdminUsersQuery(Guid RequestingUserId, string? Cursor = null, int PageSize = 50)
    : IRequest<AdminUsersDto>;
