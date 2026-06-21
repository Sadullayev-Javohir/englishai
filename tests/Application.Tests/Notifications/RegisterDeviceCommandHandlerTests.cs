using Application.Notifications.Ports;
using Application.Notifications.RegisterDevice;
using Application.Tests.Learning;
using NSubstitute;
using Xunit;

namespace Application.Tests.Notifications;

public class RegisterDeviceCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 14, 30, 0, TimeSpan.Zero);
    private readonly IDeviceTokenStore _store = Substitute.For<IDeviceTokenStore>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private RegisterDeviceCommandHandler Handler() => new(_store, _clock);

    [Fact]
    public async Task Registering_upserts_the_token_for_the_learner_with_the_current_time()
    {
        var learner = Guid.NewGuid();

        await Handler().Handle(new RegisterDeviceCommand(learner, "tok-abc", "android"), CancellationToken.None);

        await _store.Received(1).UpsertAsync(learner, "tok-abc", "android", Now, Arg.Any<CancellationToken>());
    }
}
