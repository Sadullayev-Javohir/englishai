using FluentValidation;

namespace Application.Vocabulary.ResetTopicModuleScore;

public sealed class ResetTopicModuleScoreCommandValidator : AbstractValidator<ResetTopicModuleScoreCommand>
{
    public ResetTopicModuleScoreCommandValidator()
    {
        RuleFor(command => command.LearnerId).NotEmpty();
        RuleFor(command => command.TopicId).NotEmpty();
        RuleFor(command => command.Module).IsInEnum();
    }
}
