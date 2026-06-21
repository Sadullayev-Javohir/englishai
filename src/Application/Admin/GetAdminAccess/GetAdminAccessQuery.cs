using Application.Admin.Dtos;
using MediatR;

namespace Application.Admin.GetAdminAccess;

/// <summary>
/// Resolves the signed-in user's admin standing so the SPA can decide whether to surface the admin
/// panel. <see cref="RequestingUserId"/> is the viewer (taken from the session, never the body); it is
/// deliberately NOT named <c>LearnerId</c> so the ownership pipeline behavior leaves it alone.
/// </summary>
public sealed record GetAdminAccessQuery(Guid RequestingUserId) : IRequest<AdminAccessDto>;
