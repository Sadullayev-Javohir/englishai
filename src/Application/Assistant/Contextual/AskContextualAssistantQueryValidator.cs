using FluentValidation;

namespace Application.Assistant.Contextual;

public sealed class AskContextualAssistantQueryValidator : AbstractValidator<AskContextualAssistantQuery>
{
    public AskContextualAssistantQueryValidator()
    {
        RuleFor(x => x.Area).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Title).MaximumLength(300);
        RuleFor(x => x.Context).MaximumLength(8000);
        RuleFor(x => x.FocusText).MaximumLength(3000);
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.History).NotNull().Must(x => x.Count <= 12);
    }
}
