using Domain.Assessment;
using FluentValidation;

namespace Application.Vocabulary.Admin.CreateVocabularyTopic;

public sealed class CreateVocabularyTopicCommandValidator
    : AbstractValidator<CreateVocabularyTopicCommand>
{
    public CreateVocabularyTopicCommandValidator()
    {
        RuleFor(c => c.Slug)
            .NotEmpty().WithMessage("Slug must not be empty.")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits and hyphens (e.g. \"a1-my-family\").");
        RuleFor(c => c.Title).NotEmpty().WithMessage("Title must not be empty.");
        RuleFor(c => c.TitleUz).NotEmpty().WithMessage("Uzbek title must not be empty.");
        RuleFor(c => c.Category).NotEmpty().WithMessage("Category must not be empty.");
        RuleFor(c => c.GrammarFocusCode).NotEmpty().WithMessage("Grammar focus code must not be empty.");
        RuleFor(c => c.Level)
            .NotEmpty().WithMessage("Level must not be empty.")
            .Must(BeValidCefrLevel)
            .WithMessage("Level must be a valid CEFR level (A1, A2, B1, B2, C1, C2).");
    }

    private static bool BeValidCefrLevel(string level) =>
        Enum.TryParse<CefrLevel>(level, ignoreCase: true, out _);
}
