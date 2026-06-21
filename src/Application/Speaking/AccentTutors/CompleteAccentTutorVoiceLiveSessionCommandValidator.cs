using FluentValidation;

namespace Application.Speaking.AccentTutors;

public sealed class CompleteAccentTutorVoiceLiveSessionCommandValidator
    : AbstractValidator<CompleteAccentTutorVoiceLiveSessionCommand>
{
    // Session ids are minted as Guid "N" format, so anything else is a forged or corrupted report.
    private const int SessionIdLength = 32;

    public CompleteAccentTutorVoiceLiveSessionCommandValidator()
    {
        RuleFor(x => x.TutorId)
            .NotEmpty()
            .Must(AccentTutorCatalog.IsKnown)
            .WithMessage("Unknown accent tutor.");

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .Length(SessionIdLength)
            .Must(id => id.All(Uri.IsHexDigit))
            .WithMessage("Session id is malformed.");

        RuleFor(x => x.DurationSeconds)
            .Must(double.IsFinite)
            .WithMessage("Duration must be a finite number.")
            .GreaterThanOrEqualTo(0);
    }
}
