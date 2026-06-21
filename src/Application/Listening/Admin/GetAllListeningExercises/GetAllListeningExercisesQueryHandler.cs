using Application.Common;
using Application.Identity.Dtos;
using Application.Listening.Admin;
using Application.Listening.Ports;
using Domain.Listening;
using MediatR;

namespace Application.Listening.Admin.GetAllListeningExercises;

public sealed class GetAllListeningExercisesQueryHandler
    : IRequestHandler<GetAllListeningExercisesQuery, IReadOnlyList<ListeningExerciseAdminDto>>
{
    private readonly IAdminAuthorization _admin;
    private readonly IListeningRepository _exercises;

    public GetAllListeningExercisesQueryHandler(IAdminAuthorization admin, IListeningRepository exercises)
    {
        _admin = admin;
        _exercises = exercises;
    }

    public async Task<IReadOnlyList<ListeningExerciseAdminDto>> Handle(
        GetAllListeningExercisesQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var all = await _exercises.GetAllOrderedByLevelAsync(cancellationToken);

        return all
            .OrderBy(e => e.Level)
            .ThenByDescending(e => e.CreatedAt)
            .Select(ListeningExerciseAdminDto.FromDomain)
            .ToList();
    }
}
