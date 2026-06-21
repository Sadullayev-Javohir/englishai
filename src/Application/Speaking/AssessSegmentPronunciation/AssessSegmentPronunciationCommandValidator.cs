using FluentValidation;

namespace Application.Speaking.AssessSegmentPronunciation;

public sealed class AssessSegmentPronunciationCommandValidator
    : AbstractValidator<AssessSegmentPronunciationCommand>
{
    /// <summary>Upper bound on a reference line - one transcript sentence, not a paragraph.</summary>
    public const int MaxReferenceLength = 400;

    public AssessSegmentPronunciationCommandValidator()
    {
        RuleFor(c => c.ReferenceText).NotEmpty().MaximumLength(MaxReferenceLength);
        RuleFor(c => c.AudioContent).NotNull();
    }
}
