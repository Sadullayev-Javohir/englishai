using FluentValidation;

namespace Application.Vocabulary.GetMandatoryReviewStatus;

public sealed class GetMandatoryReviewStatusQueryValidator
    : AbstractValidator<GetMandatoryReviewStatusQuery>
{
    public GetMandatoryReviewStatusQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
