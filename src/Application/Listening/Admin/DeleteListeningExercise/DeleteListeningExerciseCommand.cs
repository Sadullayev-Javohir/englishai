using MediatR;

namespace Application.Listening.Admin.DeleteListeningExercise;

/// <summary>
/// Deletes a curated listening exercise from the catalog by id. <paramref name="RequestingUserId"/> is
/// the acting admin (from the session); the handler authorizes it as an admin and rejects everyone
/// else with a 403.
/// </summary>
public sealed record DeleteListeningExerciseCommand(Guid RequestingUserId, Guid Id) : IRequest<Unit>;
