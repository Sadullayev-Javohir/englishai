using FluentValidation;

namespace Application.Grammar.CheckGrammarExercise;

public sealed class CheckGrammarExerciseQueryValidator : AbstractValidator<CheckGrammarExerciseQuery>
{
    public CheckGrammarExerciseQueryValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.ExerciseId).NotEmpty();
        RuleFor(x => x.SelectedOptionIndex).GreaterThanOrEqualTo(0).When(x => x.TextAnswer is null);
        RuleFor(x => x.TextAnswer).NotEmpty().MaximumLength(2000).When(x => x.TextAnswer is not null);
    }
}
