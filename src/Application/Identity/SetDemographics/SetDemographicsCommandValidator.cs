using Domain.Identity;
using FluentValidation;

namespace Application.Identity.SetDemographics;

public sealed class SetDemographicsCommandValidator : AbstractValidator<SetDemographicsCommand>
{
    public SetDemographicsCommandValidator(TimeProvider clock)
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BirthDate).Must(date =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            return date <= today && date >= today.AddYears(-121);
        }).WithMessage("Birth date must represent an age between 0 and 120.");
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.AcquisitionSource).IsInEnum();
        RuleFor(x => x.AcquisitionSourceOther)
            .Must(value => value is not null && value.Trim().Length is >= 2 and <= 100)
            .When(x => x.AcquisitionSource == Domain.Identity.AcquisitionSource.Other)
            .WithMessage("Other acquisition source must be between 2 and 100 characters.");
    }
}
