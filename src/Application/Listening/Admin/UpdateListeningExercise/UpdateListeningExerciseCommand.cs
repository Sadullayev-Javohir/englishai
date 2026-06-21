using Application.Listening.Admin;
using Domain.Assessment;
using MediatR;

namespace Application.Listening.Admin.UpdateListeningExercise;

/// <summary>
/// Updates a curated listening exercise's metadata (title, topic, CEFR level).
/// <paramref name="RequestingUserId"/> is the acting admin (from the session); the handler
/// authorizes it as an admin and rejects everyone else with a 403. <paramref name="Level"/>
/// is the CEFR band name (e.g. "B1") parsed to <see cref="CefrLevel"/> by the validator.
/// </summary>
public sealed record UpdateListeningExerciseCommand(
    Guid RequestingUserId,
    Guid Id,
    string Title,
    string Topic,
    string Level)
    : IRequest<ListeningExerciseAdminDto>;
