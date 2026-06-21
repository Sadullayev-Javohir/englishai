using Application.Writing.Admin;
using MediatR;

namespace Application.Writing.Admin.GetAllWritingTasks;

/// <summary>
/// Returns every writing task for the admin catalog, ordered by CEFR level (A1→C2) then most-recent
/// first within a level. <paramref name="RequestingUserId"/> is the viewer (from the session); the
/// handler authorizes it as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record GetAllWritingTasksQuery(Guid RequestingUserId)
    : IRequest<IReadOnlyList<WritingTaskAdminDto>>;
