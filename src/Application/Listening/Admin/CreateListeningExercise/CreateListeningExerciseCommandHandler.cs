using Application.Common;
using Application.Identity.Dtos;
using Application.Listening.Admin;
using Application.Listening.Ports;
using Domain.Assessment;
using Domain.Listening;
using MediatR;

namespace Application.Listening.Admin.CreateListeningExercise;

public sealed class CreateListeningExerciseCommandHandler
    : IRequestHandler<CreateListeningExerciseCommand, ListeningExerciseAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IListeningRepository _exercises;
    private readonly TimeProvider _clock;

    public CreateListeningExerciseCommandHandler(
        IAdminAuthorization admin, IListeningRepository exercises, TimeProvider clock)
    {
        _admin = admin;
        _exercises = exercises;
        _clock = clock;
    }

    public async Task<ListeningExerciseAdminDto> Handle(
        CreateListeningExerciseCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        var now = _clock.GetUtcNow();

        var exercise = ListeningExercise.CreateManual(request.Title, request.Topic, level, now);

        await _exercises.SaveAsync(exercise, cancellationToken);

        return ListeningExerciseAdminDto.FromDomain(exercise);
    }
}
