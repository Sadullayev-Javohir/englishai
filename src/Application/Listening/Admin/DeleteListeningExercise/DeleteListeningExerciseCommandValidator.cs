using FluentValidation;

namespace Application.Listening.Admin.DeleteListeningExercise;

public sealed class DeleteListeningExerciseCommandValidator : AbstractValidator<DeleteListeningExerciseCommand>
{
    public DeleteListeningExerciseCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
