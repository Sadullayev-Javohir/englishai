using Application.Admin.SetVariableCostBudget;
using Application.Ai;
using Application.Common;
using Application.Identity.Dtos;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Admin;

public sealed class SetVariableCostBudgetCommandHandlerTests
{
    [Fact]
    public async Task Super_admin_can_change_the_daily_budget()
    {
        var userId = Guid.NewGuid();
        var admin = Substitute.For<IAdminAuthorization>();
        admin.GetRoleAsync(userId, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);
        var costs = Substitute.For<IVariableCostMeter>();
        var snapshot = Snapshot(50);
        costs.Snapshot().Returns(snapshot);
        var handler = new SetVariableCostBudgetCommandHandler(admin, costs);

        var result = await handler.Handle(new SetVariableCostBudgetCommand(userId, 50), CancellationToken.None);

        await costs.Received(1).SetDailyBudgetAsync(50, Arg.Any<CancellationToken>());
        result.Should().BeSameAs(snapshot);
    }

    [Theory]
    [InlineData(AdminRole.None)]
    [InlineData(AdminRole.Admin)]
    public async Task Non_super_admin_cannot_change_the_daily_budget(AdminRole role)
    {
        var userId = Guid.NewGuid();
        var admin = Substitute.For<IAdminAuthorization>();
        admin.GetRoleAsync(userId, Arg.Any<CancellationToken>()).Returns(role);
        var costs = Substitute.For<IVariableCostMeter>();
        var handler = new SetVariableCostBudgetCommandHandler(admin, costs);

        var action = () => handler.Handle(new SetVariableCostBudgetCommand(userId, 50), CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>();
        await costs.DidNotReceive().SetDailyBudgetAsync(Arg.Any<double>(), Arg.Any<CancellationToken>());
    }

    private static VariableCostSnapshot Snapshot(double budget) => new(
        new DateOnly(2026, 8, 11),
        0,
        budget,
        false,
        false,
        true,
        Array.Empty<VariableCostCategorySnapshot>(),
        Array.Empty<VariableCostAttributionSnapshot>(),
        Array.Empty<VariableCostAttributionSnapshot>());
}
