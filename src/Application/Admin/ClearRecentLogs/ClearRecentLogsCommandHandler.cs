using Application.Admin.Ports;
using Application.Common;
using Application.Identity.Dtos;
using MediatR;

namespace Application.Admin.ClearRecentLogs;

/// <summary>
/// Empties the in-memory recent-log buffer. Restricted to the <see cref="AdminRole.SuperAdmin"/>
/// alone - ordinary admins get a 403. The acting user comes from the session, never the request.
/// </summary>
public sealed class ClearRecentLogsCommandHandler : IRequestHandler<ClearRecentLogsCommand, int>
{
    private readonly IAdminAuthorization _admin;
    private readonly IRecentLogStore _logStore;

    public ClearRecentLogsCommandHandler(IAdminAuthorization admin, IRecentLogStore logStore)
    {
        _admin = admin;
        _logStore = logStore;
    }

    public async Task<int> Handle(ClearRecentLogsCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Super-admin access is required.");

        return _logStore.Clear();
    }
}
