using FluentValidation;

namespace Application.Assistant.AskProjectAssistant;

public sealed class AskProjectAssistantQueryValidator : AbstractValidator<AskProjectAssistantQuery>
{
    public const int MaxQuestionLength = 500;
    public const int MaxHistoryTurns = 12;
    public const int MaxHistoryTextLength = 1000;

    public AskProjectAssistantQueryValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(MaxQuestionLength);
        RuleFor(x => x.Locale).Must(locale => locale is "uz" or "en");
        RuleFor(x => x.History).NotNull().Must(history => history.Count <= MaxHistoryTurns);
        RuleForEach(x => x.History).ChildRules(turn =>
        {
            turn.RuleFor(x => x.Role).Must(role => role is "user" or "assistant");
            turn.RuleFor(x => x.Text).NotEmpty().MaximumLength(MaxHistoryTextLength);
        });
    }
}
