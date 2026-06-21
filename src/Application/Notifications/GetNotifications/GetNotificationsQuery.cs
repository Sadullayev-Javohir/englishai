using Application.Notifications.Dtos;
using Application.Common;
using MediatR;

namespace Application.Notifications.GetNotifications;

/// <summary>A learner's in-app notifications, newest first (Notifications screen).</summary>
public sealed record GetNotificationsQuery(Guid LearnerId, string? Cursor = null, int PageSize = 30)
    : IRequest<CursorPage<NotificationDto>>;
