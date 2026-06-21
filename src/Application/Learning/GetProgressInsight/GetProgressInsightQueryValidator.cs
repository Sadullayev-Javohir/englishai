using FluentValidation;

namespace Application.Learning.GetProgressInsight;

public sealed class GetProgressInsightQueryValidator : AbstractValidator<GetProgressInsightQuery>
{
    public GetProgressInsightQueryValidator()
    {
        RuleFor(query => query.LearnerId).NotEmpty();
        RuleFor(query => query.Today).NotEqual(default(DateOnly));
    }
}
