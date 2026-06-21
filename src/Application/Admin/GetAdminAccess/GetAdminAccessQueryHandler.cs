using Application.Admin.Dtos;
using Application.Common;
using MediatR;

namespace Application.Admin.GetAdminAccess;

public sealed class GetAdminAccessQueryHandler : IRequestHandler<GetAdminAccessQuery, AdminAccessDto>
{
    private readonly IAdminAuthorization _admin;

    public GetAdminAccessQueryHandler(IAdminAuthorization admin)
    {
        _admin = admin;
    }

    public async Task<AdminAccessDto> Handle(GetAdminAccessQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        return new AdminAccessDto(role);
    }
}
