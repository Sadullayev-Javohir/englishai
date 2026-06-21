using Application.Common;
using Application.Identity.Dtos;
using Application.Listening.Admin;
using Application.Listening.Ports;
using Domain.Assessment;
using Domain.Listening;
using MediatR;

namespace Application.Listening.Admin.UpdateListeningExercise;

public sealed class UpdateListeningExerciseCommandHandler
    : IRequestHandler<UpdateListeningExerciseCommand, ListeningExerciseAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IListeningRepository _exercises;

    public UpdateListeningExerciseCommandHandler(IAdminAuthorization admin, IListeningRepository exercises)
    {
        _admin = admin;
        _exercises = exercises;
    }

    public async Task<ListeningExerciseAdminDto> Handle(
        UpdateListeningExerciseCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var exercise = await _exercises.GetByIdAsync(request.Id, cancellationToken)
                      ?? throw new NotFoundException(nameof(ListeningExercise), request.Id);

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);

        exercise.AdminUpdate(request.Title, request.Topic, level);

        await _exercises.SaveAsync(exercise, cancellationToken);

        return ListeningExerciseAdminDto.FromDomain(exercise);
    }
}
