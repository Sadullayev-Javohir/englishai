using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.UpdateProfile;

/// <summary>
/// Sets or changes the account's editable profile fields - display name and public username.
/// Serves both the post-sign-up handle-setup step (first time a username is chosen) and the
/// Profile screen's edit form. Returns the refreshed authenticated user.
/// </summary>
public sealed record UpdateProfileCommand(Guid UserId, string DisplayName, string Username)
    : IRequest<AuthenticatedUserDto>;
