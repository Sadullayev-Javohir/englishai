using FluentValidation;

namespace Application.Gamification.CompleteTask;

public sealed class CompleteTaskCommandValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
