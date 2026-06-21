using FluentValidation;

namespace Application.Listening.CheckListeningAnswer;

public sealed class CheckListeningAnswerQueryValidator : AbstractValidator<CheckListeningAnswerQuery>
{
    public CheckListeningAnswerQueryValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.SelectedOptionIndex).GreaterThanOrEqualTo(0);
    }
}
