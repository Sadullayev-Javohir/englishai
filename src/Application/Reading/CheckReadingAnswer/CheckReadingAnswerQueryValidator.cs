using FluentValidation;

namespace Application.Reading.CheckReadingAnswer;

public sealed class CheckReadingAnswerQueryValidator : AbstractValidator<CheckReadingAnswerQuery>
{
    public CheckReadingAnswerQueryValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.SelectedOptionIndex).GreaterThanOrEqualTo(0);
    }
}
