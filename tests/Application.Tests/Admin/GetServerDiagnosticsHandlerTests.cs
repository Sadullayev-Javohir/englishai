using Application.Admin.Dtos;
using Application.Admin.GetServerDiagnostics;
using Application.Admin.Ports;
using Application.Common;
using Application.Identity.Dtos;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Admin;

/// <summary>
/// The server-operations snapshot is the most privileged read in the admin panel, so it is gated to
/// the super-admin alone - an ordinary admin and a plain learner are both refused (403). Only when the
/// caller is the super-admin does the handler delegate to the diagnostics provider.
/// </summary>
public class GetServerDiagnosticsHandlerTests
{
    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();
    private readonly IServerDiagnosticsProvider _provider = Substitute.For<IServerDiagnosticsProvider>();

    private GetServerDiagnosticsQueryHandler Handler() => new(_admin, _provider);

    private static ServerDiagnosticsDto Snapshot() => new(
        DateTimeOffset.UnixEpoch,
        new ServerRuntimeDto("Production", "1.0", ".NET", "Linux", "host",
            DateTimeOffset.UnixEpoch, 0, 1, 0, 0, 1, 0, 0, 0),
        Array.Empty<DependencyHealthDto>(),
        Array.Empty<ExternalServiceDto>(),
        new ServerLogSummaryDto(0, 0, 100, Array.Empty<LogEntryDto>()));

    [Theory]
    [InlineData(AdminRole.None)]
    [InlineData(AdminRole.Admin)]
    public async Task Non_super_admins_are_forbidden(AdminRole role)
    {
        var userId = Guid.NewGuid();
        _admin.GetRoleAsync(userId, Arg.Any<CancellationToken>()).Returns(role);

        var act = () => Handler().Handle(new GetServerDiagnosticsQuery(userId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _provider.DidNotReceive().CollectAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_super_admin_receives_the_snapshot()
    {
        var userId = Guid.NewGuid();
        _admin.GetRoleAsync(userId, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);
        var expected = Snapshot();
        _provider.CollectAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await Handler().Handle(new GetServerDiagnosticsQuery(userId), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}
