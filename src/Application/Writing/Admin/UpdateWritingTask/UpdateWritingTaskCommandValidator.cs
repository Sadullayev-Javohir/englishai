using Domain.Assessment;
using FluentValidation;

namespace Application.Writing.Admin.UpdateWritingTask;

public sealed class UpdateWritingTaskCommandValidator
    : AbstractValidator<UpdateWritingTaskCommand>
{
    public UpdateWritingTaskCommandValidator()
    {
        RuleFor(c => c.Level)
            .NotEmpty().WithMessage("Level must not be empty.")
            .Must(BeValidCefrLevel)
            .WithMessage("Level must be a valid CEFR level (A1, A2, B1, B2, C1, C2).");
    }

    private static bool BeValidCefrLevel(string level) =>
        Enum.TryParse<CefrLevel>(level, ignoreCase: true, out _);
}
