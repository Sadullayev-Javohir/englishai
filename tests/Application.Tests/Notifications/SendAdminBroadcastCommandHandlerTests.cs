using Application.Common;
using Application.Identity.Dtos;
using Application.Notifications.Dtos;
using Application.Notifications.Ports;
using Application.Notifications.SendAdminBroadcast;
using Application.Tests.Learning;
using Domain.Notifications;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

public class SendAdminBroadcastCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 14, 30, 0, TimeSpan.Zero);
    private static readonly Guid SuperAdmin = Guid.NewGuid();

    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();
    private readonly IAdminBroadcastStore _store = Substitute.For<IAdminBroadcastStore>();
    private readonly INotificationRealtimeNotifier _realtime = Substitute.For<INotificationRealtimeNotifier>();
    private readonly IPushNotifier _push = Substitute.For<IPushNotifier>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private SendAdminBroadcastCommandHandler Handler() => new(_admin, _store, _realtime, _push, _clock);

    [Fact]
    public async Task Super_admin_broadcast_is_persisted_with_the_real_send_time()
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);

        var result = await Handler().Handle(
            new SendAdminBroadcastCommand(SuperAdmin, "Yangilik", "Yangi mavzular qo'shildi.", "/speaking"),
            CancellationToken.None);

        result.Title.Should().Be("Yangilik");
        result.CreatedAt.Should().Be(Now);
        result.LinkUrl.Should().Be("/speaking");
        await _store.Received(1).AddAsync(
            Arg.Is<AdminBroadcast>(b => b.Title == "Yangilik" && b.CreatedAt == Now), Arg.Any<CancellationToken>());
        // The broadcast is pushed to connected learners in real time (title + body + destination).
        await _realtime.Received(1).BroadcastSentAsync(
            Arg.Is<NotificationDto>(n => n.Title == "Yangilik" && n.LinkUrl == "/speaking"),
            Arg.Any<CancellationToken>());
        // And to every registered device as a native push (status bar + badge, even when app closed).
        await _push.Received(1).NotifyAllAsync(
            Arg.Is<PushMessage>(m => m.Title == "Yangilik" && m.Body == "Yangi mavzular qo'shildi." && m.LinkUrl == "/speaking"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AdminRole.Admin)]
    [InlineData(AdminRole.None)]
    public async Task Non_super_admin_cannot_broadcast(AdminRole role)
    {
        _admin.GetRoleAsync(SuperAdmin, Arg.Any<CancellationToken>()).Returns(role);

        var act = () => Handler().Handle(
            new SendAdminBroadcastCommand(SuperAdmin, "T", "B", null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _store.DidNotReceive().AddAsync(Arg.Any<AdminBroadcast>(), Arg.Any<CancellationToken>());
        await _realtime.DidNotReceive().BroadcastSentAsync(
            Arg.Any<NotificationDto>(), Arg.Any<CancellationToken>());
        await _push.DidNotReceive().NotifyAllAsync(Arg.Any<PushMessage>(), Arg.Any<CancellationToken>());
    }
}
