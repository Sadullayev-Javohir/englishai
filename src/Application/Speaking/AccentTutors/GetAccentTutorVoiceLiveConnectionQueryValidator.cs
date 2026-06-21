using FluentValidation;

namespace Application.Speaking.AccentTutors;

public sealed class GetAccentTutorVoiceLiveConnectionQueryValidator
    : AbstractValidator<GetAccentTutorVoiceLiveConnectionQuery>
{
    public GetAccentTutorVoiceLiveConnectionQueryValidator()
    {
        RuleFor(x => x.TutorId)
            .NotEmpty()
            .Must(AccentTutorCatalog.IsKnown)
            .WithMessage("Unknown accent tutor.");
    }
}
