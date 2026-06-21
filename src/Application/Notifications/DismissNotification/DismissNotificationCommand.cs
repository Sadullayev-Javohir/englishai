using MediatR;

namespace Application.Notifications.DismissNotification;

/// <summary>
/// Dismisses one notification the learner tapped: it opens its destination on the client and drops out
/// of the feed (the shared "mark all read" watermark still dismisses everything at once).
/// </summary>
public sealed record DismissNotificationCommand(Guid LearnerId, Guid NotificationId) : IRequest;
