using FluentValidation;

namespace Application.Speaking.SubmitUtterance;

public sealed class SubmitUtteranceCommandValidator : AbstractValidator<SubmitUtteranceCommand>
{
    public SubmitUtteranceCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.AudioContent).NotNull().Must(a => a.Length > 0)
            .WithMessage("Audio content must not be empty.");
        RuleFor(x => x.ConfirmedTranscript)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("Confirmed transcript must not be empty.")
            .MaximumLength(1000);
        RuleFor(x => x.ConfirmationOutcome)
            .Must(value => value is null or "candidate_selected" or "edited")
            .WithMessage("Confirmation outcome is invalid.");
        RuleFor(x => x.ConfirmationOutcome)
            .NotNull()
            .When(x => x.ConfirmedTranscript is not null);
        RuleFor(x => x.ConfirmedTranscript)
            .NotNull()
            .When(x => x.ConfirmationOutcome is not null);
    }
}
