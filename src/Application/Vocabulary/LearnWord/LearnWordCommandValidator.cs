using FluentValidation;

namespace Application.Vocabulary.LearnWord;

public sealed class LearnWordCommandValidator : AbstractValidator<LearnWordCommand>
{
    public LearnWordCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Word).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Translation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ExampleSentence).MaximumLength(500).When(x => x.ExampleSentence is not null);
        RuleFor(x => x.Source).IsInEnum();
    }
}
