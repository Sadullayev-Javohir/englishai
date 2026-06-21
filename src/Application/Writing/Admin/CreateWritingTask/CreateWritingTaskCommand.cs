using Application.Writing.Admin;
using Domain.Assessment;
using MediatR;

namespace Application.Writing.Admin.CreateWritingTask;

/// <summary>
/// Creates a new pending writing task shell. <paramref name="RequestingUserId"/> is the acting admin
/// (from the session); the handler authorizes it as an admin and rejects everyone else with a 403.
/// <paramref name="Level"/> is the CEFR band name (e.g. "B1") parsed to <see cref="CefrLevel"/> by the
/// validator; the word range is derived from <c>WritingWordRange.For(level)</c> on the server.
/// </summary>
public sealed record CreateWritingTaskCommand(Guid RequestingUserId, string Level)
    : IRequest<WritingTaskAdminDto>;
