using Application.Listening.Admin;
using MediatR;

namespace Application.Listening.Admin.GetAllListeningExercises;

/// <summary>
/// Returns every curated listening exercise for the admin catalog, ordered by CEFR level
/// (A1→C2) then most-recently-created. <paramref name="RequestingUserId"/> is the viewer (from the
/// session); the handler authorizes it as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record GetAllListeningExercisesQuery(Guid RequestingUserId)
    : IRequest<IReadOnlyList<ListeningExerciseAdminDto>>;
