using Application.Common;
using Application.Identity.Dtos;
using Application.Writing.Admin;
using Application.Writing.Ports;
using Domain.Assessment;
using Domain.Writing;
using MediatR;

namespace Application.Writing.Admin.CreateWritingTask;

public sealed class CreateWritingTaskCommandHandler
    : IRequestHandler<CreateWritingTaskCommand, WritingTaskAdminDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IWritingTaskRepository _tasks;
    private readonly TimeProvider _clock;

    public CreateWritingTaskCommandHandler(
        IAdminAuthorization admin, IWritingTaskRepository tasks, TimeProvider clock)
    {
        _admin = admin;
        _tasks = tasks;
        _clock = clock;
    }

    public async Task<WritingTaskAdminDto> Handle(
        CreateWritingTaskCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var level = Enum.Parse<CefrLevel>(request.Level, ignoreCase: true);
        var now = _clock.GetUtcNow();

        var task = WritingTask.CreateManual(level, now);

        await _tasks.SaveAsync(task, cancellationToken);

        return WritingTaskAdminDto.FromDomain(task);
    }
}
