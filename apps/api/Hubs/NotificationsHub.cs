using Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Web.Observability;

namespace Web.Hubs;

/// <summary>
/// Real-time channel for in-app notifications. Learners connect on app load and receive pushed
/// events (currently super-admin broadcasts) without polling or refreshing. The hub is receive-only -
/// clients invoke no server methods - so it stays empty; the server pushes via
/// <see cref="IHubContext{NotificationsHub}"/> from <c>SignalRNotificationNotifier</c>. Authenticated
/// like every hub (the JWT rides the HttpOnly cookie on web, the access_token query on native).
/// </summary>
[Authorize]
public sealed class NotificationsHub(ICurrentUserAccessor currentUser) : Hub
{
    /// <summary>Event name clients subscribe to for a newly sent broadcast (carries a NotificationDto).</summary>
    public const string BroadcastReceivedEvent = "BroadcastReceived";
    public const string NotificationReceivedEvent = "NotificationReceived";
    public static string UserGroup(Guid userId) => $"notifications:user:{userId:N}";
    public override async Task OnConnectedAsync()
    {
        var userId = ResourceOwnership.RequireCurrentLearner(currentUser);
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        EnglishAiTelemetry.SignalRConnections.Add(1, new KeyValuePair<string, object?>("hub", "notifications"));
        EnglishAiTelemetry.SignalRConnectionEvents.Add(1, new("hub", "notifications"), new("outcome", "connected"));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        EnglishAiTelemetry.SignalRConnections.Add(-1, new KeyValuePair<string, object?>("hub", "notifications"));
        EnglishAiTelemetry.SignalRConnectionEvents.Add(
            1,
            new("hub", "notifications"),
            new("outcome", exception is null ? "disconnected" : "failed"));
        await base.OnDisconnectedAsync(exception);
    }
}
