using Application.Ai;
using Application.Common;
using Application.Identity.Dtos;
using MediatR;

namespace Application.Admin.SetVariableCostBudget;

public sealed class SetVariableCostBudgetCommandHandler : IRequestHandler<SetVariableCostBudgetCommand, VariableCostSnapshot>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVariableCostMeter _costs;

    public SetVariableCostBudgetCommandHandler(IAdminAuthorization admin, IVariableCostMeter costs)
    {
        _admin = admin;
        _costs = costs;
    }

    public async Task<VariableCostSnapshot> Handle(
        SetVariableCostBudgetCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Only a super-admin can change the daily variable-cost budget.");

        await _costs.SetDailyBudgetAsync(request.DailyBudgetUsd, cancellationToken);
        return _costs.Snapshot();
    }
}
