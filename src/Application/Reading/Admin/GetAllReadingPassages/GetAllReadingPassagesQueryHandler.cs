using Application.Common;
using Application.Identity.Dtos;
using Application.Reading.Ports;
using MediatR;

namespace Application.Reading.Admin.GetAllReadingPassages;

public sealed class GetAllReadingPassagesQueryHandler
    : IRequestHandler<GetAllReadingPassagesQuery, IReadOnlyList<ReadingPassageAdminDto>>
{
    private readonly IAdminAuthorization _admin;
    private readonly IReadingRepository _passages;

    public GetAllReadingPassagesQueryHandler(IAdminAuthorization admin, IReadingRepository passages)
    {
        _admin = admin;
        _passages = passages;
    }

    public async Task<IReadOnlyList<ReadingPassageAdminDto>> Handle(
        GetAllReadingPassagesQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var all = await _passages.GetAllAsync(cancellationToken);

        return all
            .OrderBy(p => p.Level)
            .ThenByDescending(p => p.CreatedAt)
            .Select(ReadingPassageAdminDto.FromDomain)
            .ToList();
    }
}
