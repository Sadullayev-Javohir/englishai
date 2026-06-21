using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.GetCurrentUser;

/// <summary>Returns the signed-in user identified by the validated session token.</summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<AuthenticatedUserDto>;
