using FluentValidation;

namespace Application.Competition.Finish;

public sealed class FinishCompetitionCommandValidator
    : AbstractValidator<FinishCompetitionCommand>
{
    public FinishCompetitionCommandValidator()
    {
        RuleFor(c => c.CompetitionId).NotEmpty().WithMessage("Competition id must not be empty.");
        RuleFor(c => c.HostLearnerId).NotEmpty().WithMessage("Host learner id must not be empty.");
    }
}
