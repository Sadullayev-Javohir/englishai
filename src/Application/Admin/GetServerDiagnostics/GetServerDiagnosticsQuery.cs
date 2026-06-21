using Application.Admin.Dtos;
using MediatR;

namespace Application.Admin.GetServerDiagnostics;

/// <summary>
/// Reads a live server-health snapshot for the operations page (<c>/admin/server</c>).
/// <see cref="RequestingUserId"/> is the viewer (from the session); the handler authorizes it as the
/// super-admin and rejects everyone else - including ordinary admins - with a 403. Named
/// <c>RequestingUserId</c> (not <c>LearnerId</c>) so the ownership pipeline behavior ignores it.
/// </summary>
public sealed record GetServerDiagnosticsQuery(Guid RequestingUserId) : IRequest<ServerDiagnosticsDto>;
