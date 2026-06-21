using Application.Admin.Dtos;
using Application.Admin.Ports;
using Application.Common;
using Application.Identity.Dtos;
using MediatR;

namespace Application.Admin.GetServerDiagnostics;

/// <summary>
/// Serves the server-operations snapshot. Unlike the rest of the admin panel (which any admin may
/// read), the raw server internals - dependency reachability, recent errors, configured integrations -
/// are restricted to the <see cref="AdminRole.SuperAdmin"/> alone; ordinary admins get a 403. Once
/// authorized it delegates entirely to <see cref="IServerDiagnosticsProvider"/> (the collection is an
/// Infrastructure concern).
/// </summary>
public sealed class GetServerDiagnosticsQueryHandler
    : IRequestHandler<GetServerDiagnosticsQuery, ServerDiagnosticsDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IServerDiagnosticsProvider _diagnostics;

    public GetServerDiagnosticsQueryHandler(
        IAdminAuthorization admin,
        IServerDiagnosticsProvider diagnostics)
    {
        _admin = admin;
        _diagnostics = diagnostics;
    }

    public async Task<ServerDiagnosticsDto> Handle(
        GetServerDiagnosticsQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Super-admin access is required.");

        return await _diagnostics.CollectAsync(cancellationToken);
    }
}
