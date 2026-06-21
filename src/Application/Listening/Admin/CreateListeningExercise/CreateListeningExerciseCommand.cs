using Application.Listening.Admin;
using Domain.Assessment;
using MediatR;

namespace Application.Listening.Admin.CreateListeningExercise;

/// <summary>
/// Curates a new manual listening exercise (metadata only; the transcript/audio and questions are
/// generated later). <paramref name="RequestingUserId"/> is the acting admin (from the session); the
/// handler authorizes it as an admin and rejects everyone else with a 403. <paramref name="Level"/> is
/// the CEFR band name (e.g. "B1") parsed to <see cref="CefrLevel"/> by the validator.
/// </summary>
public sealed record CreateListeningExerciseCommand(
    Guid RequestingUserId,
    string Title,
    string Topic,
    string Level)
    : IRequest<ListeningExerciseAdminDto>;
