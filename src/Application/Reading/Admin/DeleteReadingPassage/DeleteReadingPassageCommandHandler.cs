using Application.Common;
using Application.Identity.Dtos;
using Application.Reading.Ports;
using Domain.Reading;
using MediatR;

namespace Application.Reading.Admin.DeleteReadingPassage;

public sealed class DeleteReadingPassageCommandHandler
    : IRequestHandler<DeleteReadingPassageCommand, Unit>
{
    private readonly IAdminAuthorization _admin;
    private readonly IReadingRepository _passages;

    public DeleteReadingPassageCommandHandler(IAdminAuthorization admin, IReadingRepository passages)
    {
        _admin = admin;
        _passages = passages;
    }

    public async Task<Unit> Handle(
        DeleteReadingPassageCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var passage = await _passages.GetByIdAsync(request.Id, cancellationToken)
                      ?? throw new NotFoundException(nameof(ReadingPassage), request.Id);

        await _passages.DeleteAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}
