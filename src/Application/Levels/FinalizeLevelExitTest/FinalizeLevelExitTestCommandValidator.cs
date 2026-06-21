using FluentValidation;

namespace Application.Levels.FinalizeLevelExitTest;

public sealed class FinalizeLevelExitTestCommandValidator : AbstractValidator<FinalizeLevelExitTestCommand>
{
    public FinalizeLevelExitTestCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
