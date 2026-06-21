using Application.Ai;
using MediatR;

namespace Application.Admin.SetVariableCostBudget;

public sealed record SetVariableCostBudgetCommand(
    Guid RequestingUserId,
    double DailyBudgetUsd) : IRequest<VariableCostSnapshot>;
