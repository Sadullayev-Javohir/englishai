using FluentValidation;

namespace Application.Admin.SetVariableCostBudget;

public sealed class SetVariableCostBudgetCommandValidator : AbstractValidator<SetVariableCostBudgetCommand>
{
    public SetVariableCostBudgetCommandValidator()
    {
        RuleFor(command => command.DailyBudgetUsd)
            .GreaterThan(0)
            .LessThanOrEqualTo(10_000);
    }
}
