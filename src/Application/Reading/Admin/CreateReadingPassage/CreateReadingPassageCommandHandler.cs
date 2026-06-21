using Application.Common;
using Application.Identity.Dtos;
using Application.Reading.Ports;
using Domain.Assessment;
using Domain.Reading;
using MediatR;

namespace Application.Reading.Admin.CreateReadingPassage;

public sealed class CreateReadingPassageCommandHandler
    : IRequestHandler<CreateReadingPassageCommand, ReadingPassageAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IReadingRepository _passages;
    private readonly TimeProvider _clock;

    public CreateReadingPassageCommandHandler(
        IAdminAuthorization admin, IReadingRepository passages, TimeProvider clock)
    {
        _admin = admin;
        _passages = passages;
        _clock = clock;
    }

    public async Task<ReadingPassageAdminDto> Handle(
        CreateReadingPassageCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        var now = _clock.GetUtcNow();

        var passage = ReadingPassage.CreateManual(request.Title, request.Topic, level, now);

        await _passages.SaveAsync(passage, cancellationToken);

        return ReadingPassageAdminDto.FromDomain(passage);
    }
}
