using Domain.Notifications;

namespace Application.Notifications.Dtos;

// NotificationCodes lives in the parent Application.Notifications namespace.

/// <summary>An in-app notification for the Notifications screen.</summary>
public sealed record NotificationDto(
    Guid Id,
    string Code,
    string Message,
    DateTimeOffset CreatedAt,
    bool IsRead,
    string? LinkUrl,
    string? Title = null)
{
    public static NotificationDto FromDomain(Notification notification) =>
        new(
            notification.Id,
            notification.Code,
            notification.Message,
            notification.CreatedAt,
            notification.IsRead,
            notification.LinkUrl);

    /// <summary>Projects a super-admin broadcast into a feed item (title + body, real arrival time).</summary>
    public static NotificationDto FromBroadcast(AdminBroadcast broadcast) =>
        new(
            broadcast.Id,
            NotificationCodes.AdminBroadcast,
            broadcast.Body,
            broadcast.CreatedAt,
            IsRead: false,
            broadcast.LinkUrl,
            broadcast.Title);
}
