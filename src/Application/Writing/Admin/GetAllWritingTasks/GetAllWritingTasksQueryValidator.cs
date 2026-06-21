using FluentValidation;

namespace Application.Writing.Admin.GetAllWritingTasks;

public sealed class GetAllWritingTasksQueryValidator : AbstractValidator<GetAllWritingTasksQuery>
{
    public GetAllWritingTasksQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
