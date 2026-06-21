using FluentValidation;

namespace Application.Learning.GetLearnerOverview;

public sealed class GetLearnerOverviewQueryValidator : AbstractValidator<GetLearnerOverviewQuery>
{
    public GetLearnerOverviewQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
