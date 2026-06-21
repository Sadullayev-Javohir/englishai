using FluentValidation;

namespace Application.Learning.GetPublicLearnerProgress;

public sealed class GetPublicLearnerProgressQueryValidator : AbstractValidator<GetPublicLearnerProgressQuery>
{
    public GetPublicLearnerProgressQueryValidator()
    {
        RuleFor(query => query.LearnerId).NotEmpty();
        RuleFor(query => query.Today).NotEqual(default(DateOnly));
    }
}
