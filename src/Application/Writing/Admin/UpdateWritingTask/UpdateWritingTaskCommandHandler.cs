using Application.Common;
using Application.Identity.Dtos;
using Application.Writing.Admin;
using Application.Writing.Ports;
using Domain.Assessment;
using Domain.Writing;
using MediatR;

namespace Application.Writing.Admin.UpdateWritingTask;

public sealed class UpdateWritingTaskCommandHandler
    : IRequestHandler<UpdateWritingTaskCommand, WritingTaskAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IWritingTaskRepository _tasks;

    public UpdateWritingTaskCommandHandler(IAdminAuthorization admin, IWritingTaskRepository tasks)
    {
        _admin = admin;
        _tasks = tasks;
    }

    public async Task<WritingTaskAdminDto> Handle(
        UpdateWritingTaskCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var task = await _tasks.GetByIdAsync(request.Id, cancellationToken)
                   ?? throw new NotFoundException(nameof(WritingTask), request.Id);

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        task.AdminUpdate(level);

        await _tasks.SaveAsync(task, cancellationToken);

        return WritingTaskAdminDto.FromDomain(task);
    }
}
