using FluentValidation;

namespace Application.Learning.StartLearning;

public sealed class StartLearningCommandValidator : AbstractValidator<StartLearningCommand>
{
    public StartLearningCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Level).IsInEnum();
    }
}
