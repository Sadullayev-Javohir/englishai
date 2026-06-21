using Domain.Assessment;
using FluentValidation;

namespace Application.Listening.Admin.CreateListeningExercise;

public sealed class CreateListeningExerciseCommandValidator
    : AbstractValidator<CreateListeningExerciseCommand>
{
    public CreateListeningExerciseCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().WithMessage("Title must not be empty.");
        RuleFor(c => c.Topic).NotEmpty().WithMessage("Topic must not be empty.");
        RuleFor(c => c.Level)
            .NotEmpty().WithMessage("Level must not be empty.")
            .Must(BeValidCefrLevel)
            .WithMessage("Level must be a valid CEFR level (A1, A2, B1, B2, C1, C2).");
    }

    private static bool BeValidCefrLevel(string level) =>
        Enum.TryParse<CefrLevel>(level, ignoreCase: true, out _);
}
