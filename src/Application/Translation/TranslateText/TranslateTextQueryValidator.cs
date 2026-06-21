using FluentValidation;

namespace Application.Translation.TranslateText;

public sealed class TranslateTextQueryValidator : AbstractValidator<TranslateTextQuery>
{
    /// <summary>Upper bound on a single translate request - a sentence/paragraph, not a document.</summary>
    public const int MaxLength = 2000;

    public TranslateTextQueryValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(MaxLength);
    }
}
