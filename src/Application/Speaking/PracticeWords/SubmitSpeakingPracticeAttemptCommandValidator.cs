using FluentValidation;

namespace Application.Speaking.PracticeWords;

public sealed class SubmitSpeakingPracticeAttemptCommandValidator
    : AbstractValidator<SubmitSpeakingPracticeAttemptCommand>
{
    public SubmitSpeakingPracticeAttemptCommandValidator()
    {
        RuleFor(command => command.PracticeWordId).NotEmpty();
        RuleFor(command => command.AudioContent).NotNull();
    }
}
