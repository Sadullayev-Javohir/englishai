using System.Text.Json;
using Application.Notifications.Ports;
using FluentAssertions;
using Infrastructure.Notifications.Fcm;

namespace Integration.Tests.Notifications;

public sealed class FcmNotificationAppearanceTests
{
    [Fact]
    public void BuildMessageBody_Uses_EnglishAi_Channel_Parrot_Icon_And_Custom_Reminder_Sound()
    {
        var json = FcmPushNotifier.BuildMessageBody(
            "device-token",
            new PushMessage("EnglishAI.uz", "Bugungi mashqlar tayyor", "/home"));

        using var document = JsonDocument.Parse(json);
        var androidNotification = document.RootElement
            .GetProperty("message")
            .GetProperty("android")
            .GetProperty("notification");

        androidNotification.GetProperty("channel_id").GetString().Should().Be("englishai_learning_v2");
        androidNotification.GetProperty("icon").GetString().Should().Be("ic_stat_parrot");
        androidNotification.GetProperty("color").GetString().Should().Be("#127A45");
        androidNotification.GetProperty("sound").GetString().Should().Be("englishai_reminder");
        androidNotification.TryGetProperty("default_sound", out _).Should().BeFalse();
    }
}
