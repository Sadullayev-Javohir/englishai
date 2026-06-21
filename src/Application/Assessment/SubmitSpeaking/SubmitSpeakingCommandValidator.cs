using FluentValidation;

namespace Application.Assessment.SubmitSpeaking;

public sealed class SubmitSpeakingCommandValidator : AbstractValidator<SubmitSpeakingCommand>
{
    public SubmitSpeakingCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.AudioContent)
            .NotEmpty()
            .Must(audio => audio is not null && audio.Length >= 3_200)
            .WithMessage("The speaking sample is too short to assess.");
        RuleFor(x => x.AudioContent)
            .Must(audio => audio is null || audio.Length <= 4_000_000)
            .WithMessage("The speaking sample must be no longer than two minutes.");
    }
}
