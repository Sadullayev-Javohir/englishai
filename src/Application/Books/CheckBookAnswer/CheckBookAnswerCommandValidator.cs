using FluentValidation;

namespace Application.Books.CheckBookAnswer;

public sealed class CheckBookAnswerCommandValidator : AbstractValidator<CheckBookAnswerCommand>
{
    public CheckBookAnswerCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.SelectedOptionIndex).GreaterThanOrEqualTo(0);
    }
}
