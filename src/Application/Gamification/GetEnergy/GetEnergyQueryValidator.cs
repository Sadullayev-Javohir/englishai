using FluentValidation;

namespace Application.Gamification.GetEnergy;

public sealed class GetEnergyQueryValidator : AbstractValidator<GetEnergyQuery>
{
    public GetEnergyQueryValidator() => RuleFor(query => query.LearnerId).NotEmpty();
}
