using FluentValidation;

namespace Application.Vocabulary.CheckWordPronunciation;

public sealed class CheckWordPronunciationCommandValidator : AbstractValidator<CheckWordPronunciationCommand>
{
    public CheckWordPronunciationCommandValidator()
    {
        RuleFor(c => c.Word).NotEmpty().MaximumLength(100);
        RuleFor(c => c.AudioContent).NotNull();
    }
}
