using FluentValidation;

namespace Application.Vocabulary.RecordTopicModuleScore;

public sealed class RecordTopicModuleScoreCommandValidator : AbstractValidator<RecordTopicModuleScoreCommand>
{
    public RecordTopicModuleScoreCommandValidator()
    {
        RuleFor(c => c.LearnerId).NotEmpty();
        RuleFor(c => c.TopicId).NotEmpty();
        RuleFor(c => c.Module).IsInEnum();
        RuleFor(c => c.Score).InclusiveBetween(0, 100);
    }
}
