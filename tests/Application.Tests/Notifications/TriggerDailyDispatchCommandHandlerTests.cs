using Application.Common;
using Application.Identity.Dtos;
using Application.Notifications.DispatchDailyActivityReminders;
using Application.Notifications.DispatchDueReviewNotifications;
using Application.Notifications.TriggerDailyDispatch;
using Application.Vocabulary.Dtos;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

public class TriggerDailyDispatchCommandHandlerTests
{
    private static readonly Guid SuperAdmin = Guid.NewGuid();

    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();
    private readonly ISender _sender = Substitute.For<ISender>();

    private TriggerDailyDispatchCommandHandler Handler() => new(_admin, _sender);

    [Fact]
    public async Task Runs_both_streams_and_sums_the_learners_reached()
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);
        _sender.Send(Arg.Any<DispatchDueReviewNotificationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new DispatchResultDto(NotifiedLearners: 3, TotalDueItems: 7));
        _sender.Send(Arg.Any<DispatchDailyActivityRemindersCommand>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var result = await Handler().Handle(new TriggerDailyDispatchCommand(SuperAdmin), CancellationToken.None);

        result.NotifiedLearners.Should().Be(8); // 3 SRS + 5 daily-plan
        result.TotalDueItems.Should().Be(7);
        await _sender.Received(1).Send(Arg.Any<DispatchDueReviewNotificationsCommand>(), Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(Arg.Any<DispatchDailyActivityRemindersCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AdminRole.Admin)]
    [InlineData(AdminRole.None)]
    public async Task Non_super_admin_is_forbidden_and_dispatches_nothing(AdminRole role)
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(role);

        var act = () => Handler().Handle(new TriggerDailyDispatchCommand(SuperAdmin), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _sender.DidNotReceive().Send(Arg.Any<DispatchDueReviewNotificationsCommand>(), Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().Send(Arg.Any<DispatchDailyActivityRemindersCommand>(), Arg.Any<CancellationToken>());
    }
}
