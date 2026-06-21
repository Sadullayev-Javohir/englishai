using FluentValidation;

namespace Application.Learning.RecordConfirmationTest;

public sealed class RecordConfirmationTestCommandValidator
    : AbstractValidator<RecordConfirmationTestCommand>
{
    public RecordConfirmationTestCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
