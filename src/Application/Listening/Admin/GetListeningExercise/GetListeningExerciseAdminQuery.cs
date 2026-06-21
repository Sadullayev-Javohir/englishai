using MediatR;

namespace Application.Listening.Admin.GetListeningExercise;

public sealed record GetListeningExerciseAdminQuery(Guid RequestingUserId, Guid Id)
    : IRequest<ListeningExerciseAdminDetailDto>;
