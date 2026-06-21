using MediatR;

namespace Application.Writing.Admin.DeleteWritingTask;

/// <summary>
/// Deletes a writing task from the catalog by id. <paramref name="RequestingUserId"/> is the acting
/// admin (from the session); the handler authorizes it as an admin and rejects everyone else with a 403.
/// </summary>
public sealed record DeleteWritingTaskCommand(Guid RequestingUserId, Guid Id) : IRequest<Unit>;
