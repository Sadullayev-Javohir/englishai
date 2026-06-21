using FluentValidation;

namespace Application.Competition.Join;

public sealed class JoinCompetitionCommandValidator
    : AbstractValidator<JoinCompetitionCommand>
{
    public JoinCompetitionCommandValidator()
    {
        RuleFor(c => c.CompetitionId)
            .NotEmpty().WithMessage("Competition id must not be empty.");

        RuleFor(c => c.LearnerId)
            .NotEmpty().WithMessage("Learner id must not be empty.");

        RuleFor(c => c.DisplayName)
            .NotEmpty().WithMessage("Display name must not be empty.")
            .MaximumLength(60).WithMessage("Display name is too long.");
    }
}
