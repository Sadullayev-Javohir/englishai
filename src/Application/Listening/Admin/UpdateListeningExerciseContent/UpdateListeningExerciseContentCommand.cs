using MediatR;

namespace Application.Listening.Admin.UpdateListeningExerciseContent;

public sealed record UpdateListeningExerciseContentCommand(
    Guid RequestingUserId,
    Guid Id,
    ListeningExerciseAdminFullUpdateDto Payload)
    : IRequest<ListeningExerciseAdminDetailDto>;
