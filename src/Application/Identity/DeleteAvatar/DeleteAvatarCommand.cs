using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.DeleteAvatar;

public sealed record DeleteAvatarCommand(Guid UserId, string? FallbackPictureUrl = null) : IRequest<AuthenticatedUserDto>;
