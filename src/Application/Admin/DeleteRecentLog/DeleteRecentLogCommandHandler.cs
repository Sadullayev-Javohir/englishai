using Application.Admin.Ports;
using Application.Common;
using Application.Identity.Dtos;
using MediatR;

namespace Application.Admin.DeleteRecentLog;

/// <summary>
/// Removes one entry from the in-memory recent-log buffer. Like the rest of the server-operations
/// surface, this is restricted to the <see cref="AdminRole.SuperAdmin"/> alone - ordinary admins get
/// a 403. The acting user comes from the session, never the request.
/// </summary>
public sealed class DeleteRecentLogCommandHandler : IRequestHandler<DeleteRecentLogCommand, bool>
{
    private readonly IAdminAuthorization _admin;
    private readonly IRecentLogStore _logStore;

    public DeleteRecentLogCommandHandler(IAdminAuthorization admin, IRecentLogStore logStore)
    {
        _admin = admin;
        _logStore = logStore;
    }

    public async Task<bool> Handle(DeleteRecentLogCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Super-admin access is required.");

        return _logStore.Remove(request.LogId);
    }
}
