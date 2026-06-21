using FluentValidation;

namespace Application.Analytics.GetStudyStats;

public sealed class GetStudyStatsQueryValidator : AbstractValidator<GetStudyStatsQuery>
{
    public GetStudyStatsQueryValidator()
    {
        RuleFor(q => q.LearnerId).NotEmpty();
        RuleFor(q => q.Today).NotEqual(default(DateOnly));
    }
}
