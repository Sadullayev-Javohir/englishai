using FluentValidation;

namespace Application.Assistant.AskAssistant;

public sealed class AskAssistantQueryValidator : AbstractValidator<AskAssistantQuery>
{
    public const int MaxQuestionLength = 500;
    public const int MaxHistoryTurns = 12;
    public const int MaxHistoryTextLength = 1000;

    public AskAssistantQueryValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(MaxQuestionLength);
        RuleFor(x => x.History).NotNull().Must(history => history.Count <= MaxHistoryTurns);
        RuleForEach(x => x.History).ChildRules(turn =>
        {
            turn.RuleFor(x => x.Role).Must(role => role is "user" or "assistant");
            turn.RuleFor(x => x.Text).NotEmpty().MaximumLength(MaxHistoryTextLength);
        });
    }
}
