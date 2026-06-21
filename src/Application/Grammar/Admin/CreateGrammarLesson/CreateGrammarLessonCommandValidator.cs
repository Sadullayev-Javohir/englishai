using Domain.Assessment;
using Domain.Learning;
using FluentValidation;

namespace Application.Grammar.Admin.CreateGrammarLesson;

public sealed class CreateGrammarLessonCommandValidator
    : AbstractValidator<CreateGrammarLessonCommand>
{
    public CreateGrammarLessonCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().WithMessage("Title must not be empty.");
        RuleFor(c => c.Category)
            .NotEmpty().WithMessage("Category must not be empty.")
            .Must(BeValidErrorCategory)
            .WithMessage("Category must be a valid error category (Articles, VerbTense, Prepositions, ...).");
        RuleFor(c => c.Level)
            .NotEmpty().WithMessage("Level must not be empty.")
            .Must(BeValidCefrLevel)
            .WithMessage("Level must be a valid CEFR level (A1, A2, B1, B2, C1, C2).");
    }

    private static bool BeValidErrorCategory(string category) =>
        Enum.TryParse<ErrorCategory>(category, ignoreCase: true, out _);

    private static bool BeValidCefrLevel(string level) =>
        Enum.TryParse<CefrLevel>(level, ignoreCase: true, out _);
}
