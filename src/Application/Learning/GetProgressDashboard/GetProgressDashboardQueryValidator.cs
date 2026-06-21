using FluentValidation;

namespace Application.Learning.GetProgressDashboard;

public sealed class GetProgressDashboardQueryValidator : AbstractValidator<GetProgressDashboardQuery>
{
    public GetProgressDashboardQueryValidator()
    {
        RuleFor(query => query.LearnerId).NotEmpty();
        RuleFor(query => query.Today).NotEqual(default(DateOnly));
    }
}
