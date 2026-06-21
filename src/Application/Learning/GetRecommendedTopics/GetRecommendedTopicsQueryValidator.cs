using FluentValidation;

namespace Application.Learning.GetRecommendedTopics;

public sealed class GetRecommendedTopicsQueryValidator : AbstractValidator<GetRecommendedTopicsQuery>
{
    public GetRecommendedTopicsQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 50);
    }
}
