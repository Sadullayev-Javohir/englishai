using FluentValidation;

namespace Application.Assessment.FinalizePlacementTest;

public sealed class FinalizePlacementTestCommandValidator
    : AbstractValidator<FinalizePlacementTestCommand>
{
    public FinalizePlacementTestCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
