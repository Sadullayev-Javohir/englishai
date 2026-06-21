using FluentValidation;

namespace Application.Gamification.GetGamificationStatus;

public sealed class GetGamificationStatusQueryValidator : AbstractValidator<GetGamificationStatusQuery>
{
    public GetGamificationStatusQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
