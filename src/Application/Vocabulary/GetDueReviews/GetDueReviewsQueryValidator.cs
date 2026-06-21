using FluentValidation;

namespace Application.Vocabulary.GetDueReviews;

public sealed class GetDueReviewsQueryValidator : AbstractValidator<GetDueReviewsQuery>
{
    public GetDueReviewsQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
