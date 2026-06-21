using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.UpdateAvatar;

public sealed record UpdateAvatarCommand(Guid UserId, byte[] Data, string ContentType)
    : IRequest<AuthenticatedUserDto>;
