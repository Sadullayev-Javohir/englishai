using FluentValidation;

namespace Application.Competition.Advance;

public sealed class AdvanceSlideCommandValidator
    : AbstractValidator<AdvanceSlideCommand>
{
    public AdvanceSlideCommandValidator()
    {
        RuleFor(c => c.CompetitionId).NotEmpty().WithMessage("Competition id must not be empty.");
        RuleFor(c => c.HostLearnerId).NotEmpty().WithMessage("Host learner id must not be empty.");
    }
}
