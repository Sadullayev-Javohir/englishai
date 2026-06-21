using FluentValidation;

namespace Application.Vocabulary.SubmitReview;

public sealed class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.VocabularyItemId).NotEmpty();

        // Which of the two is actually required depends on the item's mini-test type, which this
        // validator has no access to (it does not hit the repository) - the handler enforces the
        // precise per-mode requirement. This only rejects the one case that is always wrong:
        // neither an answer nor a self-rating was sent at all.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.SubmittedAnswer) || x.SelfRatedPassed.HasValue)
            .WithMessage("Either a submitted answer or a self-rated result is required.");
    }
}
