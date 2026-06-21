using FluentValidation;

namespace Application.Assessment.ReportIntegrityViolation;

public sealed class ReportPlacementIntegrityViolationCommandValidator
    : AbstractValidator<ReportPlacementIntegrityViolationCommand>
{
    private static readonly string[] AllowedReasons = ["visibility_hidden", "window_blur", "fullscreen_exit"];

    public ReportPlacementIntegrityViolationCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.IncidentId).NotEmpty();
        RuleFor(x => x.Reason).Must(reason => AllowedReasons.Contains(reason))
            .WithMessage("Unsupported placement integrity violation reason.");
    }
}
