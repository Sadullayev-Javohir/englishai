using Application.Common;
using Application.Identity.Dtos;
using Application.Listening.Ports;
using Domain.Listening;
using MediatR;

namespace Application.Listening.Admin.DeleteListeningExercise;

public sealed class DeleteListeningExerciseCommandHandler
    : IRequestHandler<DeleteListeningExerciseCommand, Unit>
{
    private readonly IAdminAuthorization _admin;
    private readonly IListeningRepository _exercises;

    public DeleteListeningExerciseCommandHandler(IAdminAuthorization admin, IListeningRepository exercises)
    {
        _admin = admin;
        _exercises = exercises;
    }

    public async Task<Unit> Handle(
        DeleteListeningExerciseCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var exercise = await _exercises.GetByIdAsync(request.Id, cancellationToken)
                       ?? throw new NotFoundException(nameof(ListeningExercise), request.Id);

        await _exercises.DeleteAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}
