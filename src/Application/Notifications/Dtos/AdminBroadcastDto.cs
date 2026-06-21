using Domain.Notifications;

namespace Application.Notifications.Dtos;

/// <summary>A sent super-admin broadcast, for the operator console.</summary>
public sealed record AdminBroadcastDto(
    Guid Id,
    string Title,
    string Body,
    string? LinkUrl,
    DateTimeOffset CreatedAt)
{
    public static AdminBroadcastDto FromDomain(AdminBroadcast broadcast) =>
        new(broadcast.Id, broadcast.Title, broadcast.Body, broadcast.LinkUrl, broadcast.CreatedAt);
}
