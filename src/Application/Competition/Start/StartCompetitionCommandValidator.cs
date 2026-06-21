using FluentValidation;

namespace Application.Competition.Start;

public sealed class StartCompetitionCommandValidator
    : AbstractValidator<StartCompetitionCommand>
{
    public StartCompetitionCommandValidator()
    {
        RuleFor(c => c.CompetitionId).NotEmpty().WithMessage("Competition id must not be empty.");
        RuleFor(c => c.HostLearnerId).NotEmpty().WithMessage("Host learner id must not be empty.");
    }
}
