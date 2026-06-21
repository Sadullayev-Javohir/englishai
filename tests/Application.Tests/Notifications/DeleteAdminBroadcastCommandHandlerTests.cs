using Application.Common;
using Application.Identity.Dtos;
using Application.Notifications.DeleteAdminBroadcast;
using Application.Notifications.Ports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

public class DeleteAdminBroadcastCommandHandlerTests
{
    private static readonly Guid SuperAdmin = Guid.NewGuid();
    private static readonly Guid BroadcastId = Guid.NewGuid();

    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();
    private readonly IAdminBroadcastStore _store = Substitute.For<IAdminBroadcastStore>();

    private DeleteAdminBroadcastCommandHandler Handler() => new(_admin, _store);

    [Fact]
    public async Task Super_admin_deletes_the_broadcast()
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);
        _store.DeleteAsync(BroadcastId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handler().Handle(
            new DeleteAdminBroadcastCommand(SuperAdmin, BroadcastId), CancellationToken.None);

        result.Should().BeTrue();
        await _store.Received(1).DeleteAsync(BroadcastId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AdminRole.Admin)]
    [InlineData(AdminRole.None)]
    public async Task Non_super_admin_cannot_delete(AdminRole role)
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(role);

        var act = () => Handler().Handle(
            new DeleteAdminBroadcastCommand(SuperAdmin, BroadcastId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _store.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
