using FluentValidation;

namespace Application.Identity.SetLearningGoal;

public sealed class SetLearningGoalCommandValidator : AbstractValidator<SetLearningGoalCommand>
{
    public SetLearningGoalCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        // Reject a forged numeric payload before it reaches the domain, so the client gets a clean
        // 400 rather than a 500. Unspecified is a valid choice (explicit "skip").
        RuleFor(x => x.Goal).IsInEnum();
    }
}
