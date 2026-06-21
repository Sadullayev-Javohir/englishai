using Domain.Assessment;
using FluentValidation;

namespace Application.Vocabulary.Admin.UpdateVocabularyTopic;

public sealed class UpdateVocabularyTopicCommandValidator
    : AbstractValidator<UpdateVocabularyTopicCommand>
{
    public UpdateVocabularyTopicCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().WithMessage("Title must not be empty.");
        RuleFor(c => c.TitleUz).NotEmpty().WithMessage("Uzbek title must not be empty.");
        RuleFor(c => c.Category).NotEmpty().WithMessage("Category must not be empty.");
        RuleFor(c => c.GrammarFocusCode).NotEmpty().WithMessage("Grammar focus code must not be empty.");
        RuleFor(c => c.Level)
            .NotEmpty().WithMessage("Level must not be empty.")
            .Must(BeValidCefrLevel)
            .WithMessage("Level must be a valid CEFR level (A1, A2, B1, B2, C1, C2).");
        RuleFor(c => c.Passage)
            .NotEmpty().When(c => c.Words.Count > 0)
            .WithMessage("Passage must not be empty when words are supplied.");
        RuleForEach(c => c.Words).ChildRules(word =>
        {
            word.RuleFor(w => w.Word).NotEmpty().MaximumLength(100);
            word.RuleFor(w => w.Translation).NotEmpty().MaximumLength(200);
            word.RuleFor(w => w.ExampleSentence).MaximumLength(2000);
        });
    }

    private static bool BeValidCefrLevel(string level) =>
        Enum.TryParse<CefrLevel>(level, ignoreCase: true, out _);
}
