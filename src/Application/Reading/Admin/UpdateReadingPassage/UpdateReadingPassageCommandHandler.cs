using Application.Common;
using Application.Identity.Dtos;
using Application.Reading.Ports;
using Domain.Assessment;
using Domain.Reading;
using MediatR;

namespace Application.Reading.Admin.UpdateReadingPassage;

public sealed class UpdateReadingPassageCommandHandler
    : IRequestHandler<UpdateReadingPassageCommand, ReadingPassageAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IReadingRepository _passages;

    public UpdateReadingPassageCommandHandler(IAdminAuthorization admin, IReadingRepository passages)
    {
        _admin = admin;
        _passages = passages;
    }

    public async Task<ReadingPassageAdminDto> Handle(
        UpdateReadingPassageCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var passage = await _passages.GetByIdAsync(request.Id, cancellationToken)
                      ?? throw new NotFoundException(nameof(ReadingPassage), request.Id);

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        passage.AdminUpdate(request.Title, request.Topic, level);

        await _passages.SaveAsync(passage, cancellationToken);

        return ReadingPassageAdminDto.FromDomain(passage);
    }
}
