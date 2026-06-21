using FluentValidation;

namespace Application.Learning.GetGrowth;

public sealed class GetGrowthQueryValidator : AbstractValidator<GetGrowthQuery>
{
    public GetGrowthQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Weeks).InclusiveBetween(1, 52);
    }
}
