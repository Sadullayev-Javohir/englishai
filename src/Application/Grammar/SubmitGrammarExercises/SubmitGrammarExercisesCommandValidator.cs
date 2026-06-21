using FluentValidation;

namespace Application.Grammar.SubmitGrammarExercises;

public sealed class SubmitGrammarExercisesCommandValidator : AbstractValidator<SubmitGrammarExercisesCommand>
{
    public SubmitGrammarExercisesCommandValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Answers).NotNull();
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.ExerciseId).NotEmpty();
            answer.RuleFor(a => a.SelectedOptionIndex).GreaterThanOrEqualTo(0).When(a => a.TextAnswer is null);
            answer.RuleFor(a => a.TextAnswer).NotEmpty().MaximumLength(2000).When(a => a.TextAnswer is not null);
        });
    }
}
