using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using FluentValidation;

namespace Application.Grammar.Admin.UpdateGrammarLessonContent;

public sealed class UpdateGrammarLessonContentCommandValidator : AbstractValidator<UpdateGrammarLessonContentCommand>
{
    public UpdateGrammarLessonContentCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Payload.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Payload.Category).Must(value => Enum.TryParse<ErrorCategory>(value, true, out _));
        RuleFor(x => x.Payload.Level).Must(value => Enum.TryParse<CefrLevel>(value, true, out _));
        RuleFor(x => x.Payload.Status).Must(value => Enum.TryParse<GrammarLessonStatus>(value, true, out _));
        RuleFor(x => x.Payload.ContextIntro).MaximumLength(2000);
        RuleFor(x => x.Payload.Explanation).MaximumLength(2000);
        RuleFor(x => x.Payload.CuratedTitleUz).MaximumLength(300);
        RuleFor(x => x.Payload.CuratedSummaryUz).MaximumLength(4000);
        RuleForEach(x => x.Payload.CuratedFormulas).NotEmpty();
        RuleForEach(x => x.Payload.CuratedRules).ChildRules(item => {
            item.RuleFor(x => x.HeadingUz).NotEmpty().MaximumLength(300);
            item.RuleFor(x => x.BodyUz).NotEmpty().MaximumLength(4000);
        });
        RuleForEach(x => x.Payload.Examples).ChildRules(item => { item.RuleFor(x => x.English).NotEmpty(); });
        RuleForEach(x => x.Payload.CommonMistakes).ChildRules(item => { item.RuleFor(x => x.Text).NotEmpty(); });
        RuleForEach(x => x.Payload.Exercises).ChildRules(item => {
            item.RuleFor(x => x.Type).Must(value => Enum.TryParse<GrammarExerciseType>(value, true, out _));
            item.RuleFor(x => x.Prompt).NotEmpty();
            item.RuleFor(x => x.Options).NotNull().Must(options => options.Count >= GrammarExercise.MinOptions);
            item.RuleFor(x => x).Must(x => x.CorrectOptionIndex >= 0 && x.CorrectOptionIndex < x.Options.Count)
                .WithMessage("Correct option index is out of range.");
        });
        RuleForEach(x => x.Payload.ApplicationTasks).ChildRules(item => {
            item.RuleFor(x => x.TargetSkill).Must(value => Enum.TryParse<SkillType>(value, true, out var skill) && skill is SkillType.Speaking or SkillType.Writing);
            item.RuleFor(x => x.Prompt).NotEmpty();
        });
        When(x => string.Equals(x.Payload.Status, nameof(GrammarLessonStatus.Filled), StringComparison.OrdinalIgnoreCase), () => {
            RuleFor(x => x.Payload.ContextIntro).NotEmpty();
            RuleFor(x => x.Payload.Explanation).NotEmpty();
            RuleFor(x => x.Payload.Exercises).NotEmpty();
        });
    }
}
