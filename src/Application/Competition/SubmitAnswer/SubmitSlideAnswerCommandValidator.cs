using FluentValidation;

namespace Application.Competition.SubmitAnswer;

public sealed class SubmitSlideAnswerCommandValidator
    : AbstractValidator<SubmitSlideAnswerCommand>
{
    public SubmitSlideAnswerCommandValidator()
    {
        RuleFor(c => c.CompetitionId).NotEmpty().WithMessage("Competition id must not be empty.");
        RuleFor(c => c.ParticipantId).NotEmpty().WithMessage("Participant id must not be empty.");
        RuleFor(c => c.SelectedOptionIndex).GreaterThanOrEqualTo(0).WithMessage("Selected option index must be non-negative.");
        RuleFor(c => c.TimeRatioRemaining).InclusiveBetween(0d, 1d).WithMessage("Time ratio must be between 0 and 1.");
    }
}
