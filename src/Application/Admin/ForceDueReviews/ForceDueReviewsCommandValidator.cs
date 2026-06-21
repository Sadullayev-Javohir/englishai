using FluentValidation;

namespace Application.Admin.ForceDueReviews;

public sealed class ForceDueReviewsCommandValidator : AbstractValidator<ForceDueReviewsCommand>
{
    public ForceDueReviewsCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
