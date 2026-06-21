using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.SetPreferredName;

/// <summary>
/// Stores the name the AI speaking tutor should address the learner by - the answer to the
/// one-time "what should I call you?" prompt. The user id comes from the session (never the
/// body), so a caller can only set their own name. Returns the refreshed authenticated user so
/// the SPA can stop showing the prompt.
/// </summary>
public sealed record SetPreferredNameCommand(Guid UserId, string PreferredName)
    : IRequest<AuthenticatedUserDto>;
