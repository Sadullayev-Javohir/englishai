using Application.Common;
using Application.Identity.Dtos;
using Application.Writing.Ports;
using Domain.Writing;
using MediatR;

namespace Application.Writing.Admin.DeleteWritingTask;

public sealed class DeleteWritingTaskCommandHandler
    : IRequestHandler<DeleteWritingTaskCommand, Unit>
{
    private readonly IAdminAuthorization _admin;
    private readonly IWritingTaskRepository _tasks;

    public DeleteWritingTaskCommandHandler(IAdminAuthorization admin, IWritingTaskRepository tasks)
    {
        _admin = admin;
        _tasks = tasks;
    }

    public async Task<Unit> Handle(
        DeleteWritingTaskCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var task = await _tasks.GetByIdAsync(request.Id, cancellationToken)
                   ?? throw new NotFoundException(nameof(WritingTask), request.Id);

        await _tasks.DeleteAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}
