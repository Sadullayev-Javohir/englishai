using FluentValidation;

namespace Application.Assessment.SubmitAnswer;

public sealed class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.SelectedOptionIndex).GreaterThanOrEqualTo(0);
    }
}
