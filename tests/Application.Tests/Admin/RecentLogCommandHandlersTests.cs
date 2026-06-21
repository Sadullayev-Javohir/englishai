using Application.Admin.ClearRecentLogs;
using Application.Admin.DeleteRecentLog;
using Application.Admin.Ports;
using Application.Common;
using Application.Identity.Dtos;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Admin;

/// <summary>
/// The recent-log dismiss/clear commands are super-admin only (like the rest of the server-operations
/// surface) and delegate to the in-memory buffer via <see cref="IRecentLogStore"/>.
/// </summary>
public class RecentLogCommandHandlersTests
{
    private static readonly Guid SuperAdmin = Guid.NewGuid();
    private static readonly Guid LogId = Guid.NewGuid();

    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();
    private readonly IRecentLogStore _store = Substitute.For<IRecentLogStore>();

    private DeleteRecentLogCommandHandler DeleteHandler() => new(_admin, _store);
    private ClearRecentLogsCommandHandler ClearHandler() => new(_admin, _store);

    [Fact]
    public async Task Super_admin_removes_one_entry()
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);
        _store.Remove(LogId).Returns(true);

        var result = await DeleteHandler().Handle(
            new DeleteRecentLogCommand(SuperAdmin, LogId), CancellationToken.None);

        result.Should().BeTrue();
        _store.Received(1).Remove(LogId);
    }

    [Fact]
    public async Task Super_admin_clears_the_buffer()
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);
        _store.Clear().Returns(4);

        var result = await ClearHandler().Handle(
            new ClearRecentLogsCommand(SuperAdmin), CancellationToken.None);

        result.Should().Be(4);
        _store.Received(1).Clear();
    }

    [Theory]
    [InlineData(AdminRole.Admin)]
    [InlineData(AdminRole.None)]
    public async Task Non_super_admin_cannot_delete_or_clear(AdminRole role)
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(role);

        var deleteAct = () => DeleteHandler().Handle(
            new DeleteRecentLogCommand(SuperAdmin, LogId), CancellationToken.None);
        var clearAct = () => ClearHandler().Handle(
            new ClearRecentLogsCommand(SuperAdmin), CancellationToken.None);

        await deleteAct.Should().ThrowAsync<ForbiddenException>();
        await clearAct.Should().ThrowAsync<ForbiddenException>();
        _store.DidNotReceive().Remove(Arg.Any<Guid>());
        _store.DidNotReceive().Clear();
    }
}
