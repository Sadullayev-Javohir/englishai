using FluentValidation;

namespace Application.Books.SubmitBookQuiz;

public sealed class SubmitBookQuizCommandValidator : AbstractValidator<SubmitBookQuizCommand>
{
    public SubmitBookQuizCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Answers).NotNull();
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId).NotEmpty();
            answer.RuleFor(a => a.SelectedOptionIndex).GreaterThanOrEqualTo(0);
        });
    }
}
