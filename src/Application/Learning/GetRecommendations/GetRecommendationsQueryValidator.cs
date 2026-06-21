using FluentValidation;

namespace Application.Learning.GetRecommendations;

public sealed class GetRecommendationsQueryValidator : AbstractValidator<GetRecommendationsQuery>
{
    public GetRecommendationsQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
