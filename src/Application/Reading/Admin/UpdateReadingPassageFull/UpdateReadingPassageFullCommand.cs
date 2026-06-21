using MediatR;

namespace Application.Reading.Admin.UpdateReadingPassageFull;

public sealed record UpdateReadingPassageFullCommand(
    Guid RequestingUserId,
    Guid Id,
    ReadingPassageAdminFullUpsertDto Payload)
    : IRequest<ReadingPassageAdminDetailDto>;
