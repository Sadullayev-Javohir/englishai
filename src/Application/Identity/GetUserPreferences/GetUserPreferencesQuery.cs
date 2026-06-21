using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.GetUserPreferences;

/// <summary>Returns the learner's saved preferences, or sensible defaults if none saved yet.</summary>
public sealed record GetUserPreferencesQuery(Guid UserId) : IRequest<UserPreferencesDto>;
