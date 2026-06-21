using FluentValidation;

namespace Application.Levels.StartLevelExitTest;

public sealed class StartLevelExitTestCommandValidator : AbstractValidator<StartLevelExitTestCommand>
{
    public StartLevelExitTestCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
