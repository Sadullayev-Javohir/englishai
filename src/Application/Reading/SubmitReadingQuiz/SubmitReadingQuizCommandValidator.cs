using FluentValidation;

namespace Application.Reading.SubmitReadingQuiz;

public sealed class SubmitReadingQuizCommandValidator : AbstractValidator<SubmitReadingQuizCommand>
{
    public SubmitReadingQuizCommandValidator()
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
