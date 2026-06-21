using Application.Writing.Admin;
using Domain.Assessment;
using MediatR;

namespace Application.Writing.Admin.UpdateWritingTask;

/// <summary>
/// Updates a writing task's level (and recomputes its word range). <paramref name="RequestingUserId"/>
/// is the acting admin (from the session); the handler authorizes it as an admin and rejects everyone
/// else with a 403. <paramref name="Level"/> is the CEFR band name (e.g. "B1") parsed to
/// <see cref="CefrLevel"/> by the validator.
/// </summary>
public sealed record UpdateWritingTaskCommand(Guid RequestingUserId, Guid Id, string Level)
    : IRequest<WritingTaskAdminDto>;
