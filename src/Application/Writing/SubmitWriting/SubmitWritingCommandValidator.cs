using FluentValidation;

namespace Application.Writing.SubmitWriting;

public sealed class SubmitWritingCommandValidator : AbstractValidator<SubmitWritingCommand>
{
    public SubmitWritingCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(10000);
    }
}
