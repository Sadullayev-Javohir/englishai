using FluentValidation;

namespace Application.Video.SubmitVideoQuiz;

public sealed class SubmitVideoQuizCommandValidator : AbstractValidator<SubmitVideoQuizCommand>
{
    public SubmitVideoQuizCommandValidator()
    {
        RuleFor(x => x.VideoLessonId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty();
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId).NotEmpty();
            answer.RuleFor(a => a.SelectedOptionIndex).GreaterThanOrEqualTo(0);
        });
    }
}
