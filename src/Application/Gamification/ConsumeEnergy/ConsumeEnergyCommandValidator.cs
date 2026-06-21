using FluentValidation;

namespace Application.Gamification.ConsumeEnergy;

public sealed class ConsumeEnergyCommandValidator : AbstractValidator<ConsumeEnergyCommand>
{
    public ConsumeEnergyCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.ReferenceId).NotEmpty().MaximumLength(128);
    }
}
