using MediatR;

namespace Application.Reading.Admin.GetReadingPassageAdmin;

public sealed record GetReadingPassageAdminQuery(Guid RequestingUserId, Guid Id)
    : IRequest<ReadingPassageAdminDetailDto>;
