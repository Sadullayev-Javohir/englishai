using FluentValidation;

namespace Application.Writing.Admin.DeleteWritingTask;

public sealed class DeleteWritingTaskCommandValidator : AbstractValidator<DeleteWritingTaskCommand>
{
    public DeleteWritingTaskCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
