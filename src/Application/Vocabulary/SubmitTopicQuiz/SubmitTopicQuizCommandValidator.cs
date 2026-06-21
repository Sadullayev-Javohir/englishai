using FluentValidation;

namespace Application.Vocabulary.SubmitTopicQuiz;

public sealed class SubmitTopicQuizCommandValidator : AbstractValidator<SubmitTopicQuizCommand>
{
    public SubmitTopicQuizCommandValidator()
    {
        RuleFor(c => c.TopicId).NotEmpty();
        RuleFor(c => c.Answers).NotNull();
        RuleFor(c => c.LearnerId!.Value).NotEmpty().When(c => c.LearnerId.HasValue);
    }
}
