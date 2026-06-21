using FluentValidation;

namespace Application.Assessment.SubmitWriting;

public sealed class SubmitWritingCommandValidator : AbstractValidator<SubmitWritingCommand>
{
    public SubmitWritingCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(5_000);
    }
}
