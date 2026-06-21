using FluentValidation;

namespace Application.Listening.SubmitListeningQuiz;

public sealed class SubmitListeningQuizCommandValidator : AbstractValidator<SubmitListeningQuizCommand>
{
    public SubmitListeningQuizCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Answers).NotNull();
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId).NotEmpty();
            answer.RuleFor(a => a.SelectedOptionIndex).GreaterThanOrEqualTo(0);
        });
    }
}
