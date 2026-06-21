using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.CheckUsernameAvailability;

/// <summary>
/// Live check for the username-setup and profile-edit screens. <see cref="ExcludingUserId"/>
/// lets a user keep their own current handle (a change form should not report it as "taken").
/// </summary>
public sealed record CheckUsernameAvailabilityQuery(string Username, Guid ExcludingUserId)
    : IRequest<UsernameAvailabilityDto>;
