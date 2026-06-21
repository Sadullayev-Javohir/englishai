using FluentValidation;

namespace Application.Writing.GetWritingTask;

public sealed class GetWritingTaskQueryValidator : AbstractValidator<GetWritingTaskQuery>
{
    public GetWritingTaskQueryValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
    }
}
