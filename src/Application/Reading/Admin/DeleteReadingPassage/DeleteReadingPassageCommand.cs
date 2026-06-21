using MediatR;

namespace Application.Reading.Admin.DeleteReadingPassage;

/// <summary>
/// Deletes a curated reading passage from the catalog by id. <c>RequestingUserId</c> is the acting
/// admin (from the session); the handler authorizes it as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record DeleteReadingPassageCommand(Guid RequestingUserId, Guid Id) : IRequest<Unit>;
