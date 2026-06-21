using Application.Common;
using Application.Identity.Dtos;
using Application.Reading.Ports;
using Domain.Reading;
using MediatR;

namespace Application.Reading.Admin.GetReadingPassageAdmin;

public sealed class GetReadingPassageAdminQueryHandler
    : IRequestHandler<GetReadingPassageAdminQuery, ReadingPassageAdminDetailDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IReadingRepository _passages;

    public GetReadingPassageAdminQueryHandler(IAdminAuthorization admin, IReadingRepository passages)
    {
        _admin = admin;
        _passages = passages;
    }

    public async Task<ReadingPassageAdminDetailDto> Handle(
        GetReadingPassageAdminQuery request, CancellationToken cancellationToken)
    {
        if (await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken) is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var passage = await _passages.GetByIdAsync(request.Id, cancellationToken)
                      ?? throw new NotFoundException(nameof(ReadingPassage), request.Id);
        return ReadingPassageAdminDetailDto.FromDomain(passage);
    }
}
