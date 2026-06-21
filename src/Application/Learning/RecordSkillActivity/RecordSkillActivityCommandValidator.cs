using FluentValidation;

namespace Application.Learning.RecordSkillActivity;

public sealed class RecordSkillActivityCommandValidator : AbstractValidator<RecordSkillActivityCommand>
{
    public RecordSkillActivityCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Skill).IsInEnum();
        RuleFor(x => x.Score).InclusiveBetween(0, 100);
        RuleForEach(x => x.Errors).IsInEnum().When(x => x.Errors is not null);
    }
}
