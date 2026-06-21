using Application.Common;
using Application.Identity.Dtos;
using Application.Writing.Admin;
using Application.Writing.Ports;
using Domain.Writing;
using MediatR;

namespace Application.Writing.Admin.GetAllWritingTasks;

public sealed class GetAllWritingTasksQueryHandler
    : IRequestHandler<GetAllWritingTasksQuery, IReadOnlyList<WritingTaskAdminDto>>
{
    private readonly IAdminAuthorization _admin;
    private readonly IWritingTaskRepository _tasks;

    public GetAllWritingTasksQueryHandler(IAdminAuthorization admin, IWritingTaskRepository tasks)
    {
        _admin = admin;
        _tasks = tasks;
    }

    public async Task<IReadOnlyList<WritingTaskAdminDto>> Handle(
        GetAllWritingTasksQuery request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var all = await _tasks.GetAllAsync(cancellationToken);

        return all
            .OrderBy(t => t.Level)
            .ThenByDescending(t => t.CreatedAt)
            .Select(WritingTaskAdminDto.FromDomain)
            .ToList();
    }
}
