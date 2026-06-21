using Application.Notifications.MarkNotificationsRead;
using Application.Notifications.Ports;
using Application.Tests.Learning;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

public class MarkNotificationsReadCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly INotificationReadStateStore _readState = Substitute.For<INotificationReadStateStore>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    [Fact]
    public async Task Advances_the_read_watermark_to_now_and_flags_dispatched_notifications()
    {
        var handler = new MarkNotificationsReadCommandHandler(_dispatcher, _readState, _clock);

        await handler.Handle(new MarkNotificationsReadCommand(Learner), CancellationToken.None);

        await _readState.Received(1).SetLastReadAtAsync(Learner, Now, Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).MarkAllReadAsync(Learner, Arg.Any<CancellationToken>());
    }
}
