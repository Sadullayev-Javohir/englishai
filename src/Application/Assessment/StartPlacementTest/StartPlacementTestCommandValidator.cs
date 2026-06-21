using FluentValidation;

namespace Application.Assessment.StartPlacementTest;

public sealed class StartPlacementTestCommandValidator : AbstractValidator<StartPlacementTestCommand>
{
    public StartPlacementTestCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
