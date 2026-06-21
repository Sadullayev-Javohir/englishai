using FluentValidation;

namespace Application.Gamification.GetPointsBalance;

public sealed class GetPointsBalanceQueryValidator : AbstractValidator<GetPointsBalanceQuery>
{
    public GetPointsBalanceQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
