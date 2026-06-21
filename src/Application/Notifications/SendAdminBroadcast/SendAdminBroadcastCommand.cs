using Application.Notifications.Dtos;
using MediatR;

namespace Application.Notifications.SendAdminBroadcast;

/// <summary>
/// A super-admin composes a notification (title + body, optional in-app destination) and sends it to
/// every learner. The acting user comes from the session, never the request body (docs/development-guide.md §13).
/// </summary>
public sealed record SendAdminBroadcastCommand(
    Guid RequestingUserId,
    string Title,
    string Body,
    string? LinkUrl) : IRequest<AdminBroadcastDto>;
